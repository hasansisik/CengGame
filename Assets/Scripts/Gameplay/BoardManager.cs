using System.Collections.Generic;
using UnityEngine;
using Harfpoly.Gameplay;
using TMPro;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    // ---------------- UI ----------------
    [Header("UI")]
    public TextMeshProUGUI turnLabel;      // Canvas > TurnLabel
    public TextMeshProUGUI messageLabel;   // Canvas > MessageLabel
    [SerializeField] private Color activeColor = new Color(1f, 0.82f, 0.2f); // sırası gelenin rengi
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.65f); // diğeri soluk
    [SerializeField] private string player1Name = "Player 1"; // kahverengi
    [SerializeField] private string player2Name = "Player 2"; // krem
    [SerializeField] private float messageDuration = 4f;

    private float messageHideAt = -1f;

    // ------------- Input --------------
    [Header("Input")]
    public Camera cam;
    [Tooltip("Tıklanabilir katmanlar: zemin + taşlar")]
    public LayerMask clickableMask = ~0;

    private Piece selectedPiece;

    // ------------- Takımlar --------------
    [Header("Takımlar")]
    public Piece[] myPieces;         // kahverengi = Player 1
    public Piece[] opponentPieces;   // krem      = Player 2

    [Header("Başlangıç Sıraları (ÇAKIŞMAMALI)")]
    public int[] myRows = new int[] { 0, 1 };   // alt 2 sıra
    public int[] oppRows = new int[] { 6, 7 };  // üst 2 sıra

    [Header("Grid Boyutu")]
    private const int ColCount = 4;
    private const int RowCount = 8;

    private Piece[,] grid = new Piece[RowCount, ColCount];

    // ------------- Dice --------------
    [Header("Dice")]
    public Dice dice;   // Scene'deki Dice objesini buraya sürükle

    // ------------- Turn/Zar Durumları --------------
    private enum Turn { My, Opponent }  // My = Player1 (kahverengi), Opponent = Player2 (krem)
    [SerializeField] private Turn turn = Turn.My; // runtime'da Start'ta rastgele belirlenecek

    private enum MoveOwner { Current, Opponent }

    private class MoveBudget
    {
        public MoveOwner owner;
        public int remaining;
        public PieceKind? requiredKind; // null => herhangi bir taş
        public MoveBudget(MoveOwner owner, int count, PieceKind? reqKind)
        { this.owner = owner; remaining = count; requiredKind = reqKind; }
    }

    private readonly Queue<MoveBudget> moveQueue = new Queue<MoveBudget>();
    private bool needRoll = true;            // sıradaki hamle için önce zar atılmalı mı?
    private bool smilePendingReroll = false; // gülen yüz geldiyse bir sonraki atış "özel"
    private bool waitingDiceResult = false;  // fiziksel zar atıldı, sonuç bekleniyor

    // ---- Oto pas kontrolü için 8 yön vektörleri ----
    private static readonly (int di, int dj)[] OneStepDirs = new (int, int)[]
    {
        (-1,-1), (-1,0), (-1,1),
        ( 0,-1),          ( 0,1),
        ( 1,-1), ( 1,0),  ( 1,1)
    };

    // ------------------- START -------------------
    private void Start()
    {
        // listeleri temizle/tekilleştir
        ValidateAndFixPieceLists();

        if (RowsOverlap(myRows, oppRows))
            Debug.LogError("[BoardManager] myRows ve oppRows çakışıyor! Aynı satırlar kullanılamaz.");

        // başlangıç yerleşimi
        SafeShuffleGroup(myPieces, myRows, "My");
        SafeShuffleGroup(opponentPieces, oppRows, "Opp");
        RebuildGrid();

        // --- İlk oynayacak oyuncuyu rastgele belirle (TEK ATAMA) ---
        Random.InitState(System.Environment.TickCount + GetInstanceID());
        turn = (Random.Range(0, 2) == 0) ? Turn.My : Turn.Opponent;
        UpdateTurnLabel();
        ShowMessage($"{CurrentPlayerDisplayName()} başlıyor!", messageDuration);

        // Bu tur için zar bekleme durumları
        needRoll = true;
        moveQueue.Clear();
        smilePendingReroll = false;
        waitingDiceResult = false;
        if (selectedPiece != null) { selectedPiece.DeselectPiece(); selectedPiece = null; }

        // Zar event'ine abone ol
        if (dice == null)
        {
            Debug.LogError("[BoardManager] Dice referansı bağlanmadı!");
        }
        else
        {
            dice.OnRolled += OnDiceRolled;
        }

        Debug.Log($"[START] İlk sıra: {turn}. Zar atmak için SPACE/R.");
    }

    private void OnDestroy()
    {
        if (dice != null) dice.OnRolled -= OnDiceRolled;
    }

    // ------------------- UPDATE -------------------
    private void Update()
    {
        // Mesaj zamanlayıcı
        if (messageLabel != null && messageLabel.gameObject.activeSelf && Time.time >= messageHideAt && messageHideAt > 0f)
            messageLabel.gameObject.SetActive(false);

        // --- MOUSE ile ZAR ATMA (her zaman çalışsın) ---
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray0 = cam.ScreenPointToRay(Input.mousePosition);

            // Not: Maske kullanmıyoruz ki dice farklı layer'da da olsa görülsün.
            if (Physics.Raycast(ray0, out RaycastHit hit0, 1000f))
            {
                // Debug için açıp bakabilirsin:
                // Debug.Log($"[DiceClick] Hit: {hit0.collider.name} | Layer: {LayerMask.LayerToName(hit0.collider.gameObject.layer)}");

                // Tıklanan şey Dice mı? (child/parent fark etmez)
                var diceHit = hit0.collider.GetComponentInParent<Dice>();
                if (diceHit != null && diceHit == dice)
                {
                    TryRollDice();   // Kuralları BoardManager kontrol ediyor
                    return;          // Zara tıklandıysa kalan akışı çalıştırma
                }
            }
        }

        // --- Klavye ile zar atma (Space veya R) ---
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
        {
            TryRollDice();
            return;
        }

        // Zar atılmadan ya da sonuç beklerken taş seçme/oynatma kapalı
        if (needRoll || waitingDiceResult) return;

        // --- Taş seçme / kareye gitme ---
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, clickableMask)) return;

        // 1) Taşa tıklandıysa -> uygun taş mı?
        if (hit.collider.TryGetComponent(out Piece clickedPiece))
        {
            if (!CanSelectThisPiece(clickedPiece)) return;

            if (selectedPiece != null) selectedPiece.DeselectPiece();
            selectedPiece = clickedPiece;
            selectedPiece.SelectPiece();
            return;
        }

        // 2) Kareye tıklandıysa -> seçili taş tek kare ve boş hedefe gidebiliyor mu?
        if (selectedPiece == null) return;
        if (!TryGetGridIndexFromHit(hit.point, out int ti, out int tj)) return;

        int dx = ti - selectedPiece.x;
        int dy = tj - selectedPiece.y;

        if (!IsOneStep(dx, dy)) return;   // yalnız 1 kare
        if (!InBounds(ti, tj)) return;    // sınır kontrol

        // Hamleden önce grid’i güncelle
        RecomputeGridOccupancy();

        // Hedef DOLU ise gitme
        if (grid[ti, tj] != null)
        {
            Debug.LogWarning($"Hedef kare dolu: ({ti},{tj}) -> {grid[ti, tj].name}");
            return;
        }

        // Hamle
        MoveSelectedPiece(ti, tj);

        // Seçim rengini kapat
        selectedPiece.DeselectPiece();
        selectedPiece = null;

        // Hamle bütçesinden düş
        SpendMoveAndAdvance();
    }


    // =================== UI HELPERLAR ===================

    private string CurrentPlayerDisplayName()
    {
        return (turn == Turn.My) ? player1Name : player2Name;
    }

    private void UpdateTurnLabel()
    {
        if (turnLabel == null) return;

        string act = ColorUtility.ToHtmlStringRGB(activeColor);
        string pas = ColorUtility.ToHtmlStringRGB(inactiveColor);

        if (turn == Turn.My)
            turnLabel.text = $"<color=#{act}>{player1Name}</color> / <color=#{pas}>{player2Name}</color>";
        else
            turnLabel.text = $"<color=#{pas}>{player1Name}</color> / <color=#{act}>{player2Name}</color>";
    }

    private void ShowMessage(string text, float duration)
    {
        if (messageLabel == null) return;
        messageLabel.gameObject.SetActive(true);
        messageLabel.text = text;
        messageHideAt = Time.time + Mathf.Max(0.01f, duration);
    }

    // =================== ZAR & HAMLE BÜTÇESİ ===================

    private void TryRollDice()
    {
        if (dice == null)
        {
            Debug.LogError("[Dice] BoardManager.dice atanmadı! Scene'deki Dice objesini Inspector'da bağla.");
            return;
        }

        if (!needRoll && moveQueue.Count > 0)
        {
            var peek = moveQueue.Peek();
            Debug.Log($"[Dice] Şu an hamlelerin bitmedi. Kalan: {peek.remaining} (owner={peek.owner}, kind={peek.requiredKind?.ToString() ?? "Any"})");
            return;
        }

        if (waitingDiceResult || dice.IsRolling) return;

        waitingDiceResult = true;
        dice.ThrowDice(); // fiziksel zarı fırlat
    }

    private void OnDiceRolled(DiceFaces face)
    {
        waitingDiceResult = false;
        Debug.Log($"[Dice->Board] Geldi: {face}");

        if (smilePendingReroll)
        {
            // Gülen yüz sonrası tekrar atış SONUCUNU işliyoruz
            smilePendingReroll = false;

            if (face == DiceFaces.P)
            {
                ShowMessage("Gülen yüz sonra P geldi! Bu el oynanamaz, sıra karşıya geçti.", messageDuration);
                EndTurn();
                return;
            }
            else if (face == DiceFaces.G)
            {
                // Kendi 2 hamle (ANY), rakibe 1 hamle (ANY)
                ShowMessage("Gülen yüz'den sonra yine Gülen yüz! 2 hamle (ANY) + rakipten 1 hamle (ANY).", messageDuration);
                QueueSmileAnyMoves();
                AutoAdvanceIfNoMoves();
                return;
            }
            else
            {
                // ŞEKİL: SADECE mevcut oyuncu 2 hamle (o sembol), rakip hamlesi yok
                ShowMessage($"Gülen yüz sonra {face}! Bu sembolden 2 hamle hakkın var.", messageDuration);
                QueueSmileSymbolMoves(face);  // <-- Artık sadece CURRENT 2 ekliyor
                AutoAdvanceIfNoMoves();
                return;
            }
        }

        // Normal atış
        switch (face)
        {
            case DiceFaces.P:
                ShowMessage("Pas! Sıra karşıya geçti.", messageDuration);
                EndTurn();
                break;

            case DiceFaces.G:
                ShowMessage("Gülen yüz! Tekrar zar at.", messageDuration);
                smilePendingReroll = true;
                needRoll = true; // tekrar atış bekleniyor (manuel)
                break;

            default:
                // Sembol: o sembolden 1 hamle (sadece current owner)
                QueueSingleSymbolMove(face);
                AutoAdvanceIfNoMoves();
                break;
        }
    }

    private void QueueSingleSymbolMove(DiceFaces face)
    {
        PieceKind req = FaceToKind(face);
        moveQueue.Clear();
        moveQueue.Enqueue(new MoveBudget(MoveOwner.Current, 1, req));
    }

    // >>> DEĞİŞTİ: Artık sadece mevcut oyuncuya 2 hamle (o sembol). Rakip hamlesi YOK.
    private void QueueSmileSymbolMoves(DiceFaces face)
    {
        PieceKind req = FaceToKind(face);
        moveQueue.Clear();
        moveQueue.Enqueue(new MoveBudget(MoveOwner.Current, 2, req));  // current 2
        // (önceden buradaydı) moveQueue.Enqueue(new MoveBudget(MoveOwner.Opponent, 1, req));  // SİLİNDİ
    }

    private void QueueSmileAnyMoves()
    {
        moveQueue.Clear();
        moveQueue.Enqueue(new MoveBudget(MoveOwner.Current, 2, null));   // herhangi iki taş
        moveQueue.Enqueue(new MoveBudget(MoveOwner.Opponent, 1, null));  // rakipten herhangi bir taş
    }

    private void SpendMoveAndAdvance()
    {
        if (moveQueue.Count == 0)
        {
            EndTurn();
            return;
        }

        var budget = moveQueue.Peek();
        budget.remaining--;

        if (budget.remaining <= 0)
            moveQueue.Dequeue();

        if (moveQueue.Count > 0)
        {
            // Sıradaki hamle oynanabilir mi? Değilse otomatik atla/bitir
            AutoAdvanceIfNoMoves();
            return;
        }

        // Tüm hamleler bitti → el bitti
        EndTurn();
    }

    private void EndTurn()
    {
        moveQueue.Clear();
        needRoll = true;
        smilePendingReroll = false;

        // sırayı değiştir
        SwitchTurn();

        // görsel seçim temizliği
        if (selectedPiece != null) { selectedPiece.DeselectPiece(); selectedPiece = null; }

        // UI
        UpdateTurnLabel();
        ShowMessage($"Sıra {CurrentPlayerDisplayName()}’de!", messageDuration);

        Debug.Log($"[TURN] Sıra: {turn}. Zar atmak için SPACE/R.");
    }

    private void AnnounceQueue()
    {
        var b = moveQueue.Peek();
        string who = (b.owner == MoveOwner.Current) ? "KENDİ" : "RAKİP adına";
        string sym = b.requiredKind.HasValue ? b.requiredKind.Value.ToString() : "ANY";
        Debug.Log($"[MOVE] {who} {b.remaining} hamle kaldı. Gerekli sembol: {sym}.");
    }

    private PieceKind FaceToKind(DiceFaces f)
    {
        switch (f)
        {
            case DiceFaces.Kare: return PieceKind.Square;
            case DiceFaces.Ucgen: return PieceKind.Triangle;
            case DiceFaces.X: return PieceKind.X;
            case DiceFaces.Daire: return PieceKind.Circle;
            default:
                Debug.LogError($"FaceToKind hatalı çağrı: {f}");
                return PieceKind.Square;
        }
    }

    private bool CanSelectThisPiece(Piece p)
    {
        if (needRoll || waitingDiceResult) return false;
        if (moveQueue.Count == 0) return false;

        var b = moveQueue.Peek();

        // Hamleyi kimin adına yapıyoruz?
        bool belongs =
            (b.owner == MoveOwner.Current) ? BelongsToArray(p, (turn == Turn.My) ? myPieces : opponentPieces)
                                           : BelongsToArray(p, (turn == Turn.My) ? opponentPieces : myPieces);

        if (!belongs) return false;

        // Sembol kısıtı (varsa)
        if (b.requiredKind.HasValue && p.kind != b.requiredKind.Value)
            return false;

        return true;
    }

    // -------------------- Oto Pas Yardımcıları --------------------

    // Sıradaki bütçede (moveQueue.Peek) oynanabilir en az bir hamle var mı?
    private bool HasAnyLegalMove(MoveBudget budget)
    {
        // Güncel doluluk tablosunu oluştur
        RecomputeGridOccupancy();

        // Bu hamle kimin adına? (Current = sıradaki oyuncu, Opponent = rakip adına)
        Piece[] pool = (budget.owner == MoveOwner.Current)
            ? ((turn == Turn.My) ? myPieces : opponentPieces)
            : ((turn == Turn.My) ? opponentPieces : myPieces);

        if (pool == null) return false;

        foreach (var p in pool)
        {
            if (p == null) continue;

            // Sembol kısıtı varsa uyuşmalı
            if (budget.requiredKind.HasValue && p.kind != budget.requiredKind.Value)
                continue;

            // Etrafındaki 8 kareden en az biri boş mu?
            for (int k = 0; k < OneStepDirs.Length; k++)
            {
                int ti = p.x + OneStepDirs[k].di;
                int tj = p.y + OneStepDirs[k].dj;
                if (!InBounds(ti, tj)) continue;
                if (grid[ti, tj] == null) // boş kare
                    return true;
            }
        }
        return false;
    }

    // Sıradaki hamleyi/hamleleri oynanabilirlik açısından kontrol eder.
    // Oynanamazsa moveQueue'dan düşer; hepsi bitmişse sırayı rakibe verir.
    // En az bir oynanabilir hamle kalırsa onu duyurur.
    private void AutoAdvanceIfNoMoves()
    {
        bool skippedAny = false;

        while (moveQueue.Count > 0 && !HasAnyLegalMove(moveQueue.Peek()))
        {
            var b = moveQueue.Peek();
            string who = (b.owner == MoveOwner.Current) ? "KENDİ" : "RAKİP adına";
            string sym = b.requiredKind.HasValue ? b.requiredKind.Value.ToString() : "ANY";
            Debug.Log($"[AUTO PASS] {who} {sym} için oynanabilir hamle yok. Bu hamle atlanıyor.");
            moveQueue.Dequeue();
            skippedAny = true;
        }

        if (moveQueue.Count == 0)
        {
            if (skippedAny)
                Debug.Log("[AUTO PASS] Bu elde hiç oynanabilir hamle kalmadı. Sıra rakibe geçti.");
            EndTurn(); // SwitchTurn + temizlemeler
            return;
        }

        // Buraya geldiysek en az bir oynanabilir hamle var
        needRoll = false; // artık seçim/hamle bekliyoruz
        AnnounceQueue();
    }

    // -------------------- Yardımcılar --------------------

    private bool BelongsToArray(Piece p, Piece[] arr)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == p) return true;
        return false;
    }

    private void SwitchTurn()
    {
        turn = (turn == Turn.My) ? Turn.Opponent : Turn.My;
    }

    private bool IsOneStep(int dx, int dy)
    {
        // Chebyshev distance = 1 → 8 yönde tek kare
        return Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1 && (dx != 0 || dy != 0);
    }

    private bool InBounds(int i, int j)
    {
        return i >= 0 && i < RowCount && j >= 0 && j < ColCount;
    }

    private bool RowsOverlap(int[] a, int[] b)
    {
        if (a == null || b == null) return false;
        for (int i = 0; i < a.Length; i++)
            for (int j = 0; j < b.Length; j++)
                if (a[i] == b[j]) return true;
        return false;
    }

    private void MoveSelectedPiece(int ti, int tj)
    {
        // Eski kareyi boşalt
        grid[selectedPiece.x, selectedPiece.y] = null;

        // Yeni index ata
        selectedPiece.x = ti;
        selectedPiece.y = tj;

        // Taşı hedefe gönder
        Vector2 targetPos = selectedPiece.GetGridPosition(ti, tj);
        selectedPiece.MoveTo(targetPos);

        // Yeni kareyi doldur
        grid[ti, tj] = selectedPiece;
    }

    // -------------------- Liste Doğrulama / Shuffle / Grid --------------------

    private void ValidateAndFixPieceLists()
    {
        static Piece[] CleanUnique(Piece[] src)
        {
            var set = new HashSet<Piece>();
            var list = new List<Piece>();
            if (src != null)
            {
                foreach (var p in src)
                {
                    if (p == null) continue;
                    if (set.Add(p)) list.Add(p); // aynı referansı bir kere ekle
                }
            }
            return list.ToArray();
        }

        myPieces = CleanUnique(myPieces);
        opponentPieces = CleanUnique(opponentPieces);

        // ÇAPRAZ: aynı taş iki listede ise Opp'tan çıkar
        var mySet = new HashSet<Piece>(myPieces);
        var oppNew = new List<Piece>();
        foreach (var p in opponentPieces)
        {
            if (mySet.Contains(p))
            {
                Debug.LogWarning($"[Validate] {p.name} hem My hem Opp listesinde. Opp listesinden çıkarıldı.");
                continue;
            }
            oppNew.Add(p);
        }
        opponentPieces = oppNew.ToArray();
    }

    private struct Slot { public int i, j; public Vector2 pos; }

    private void SafeShuffleGroup(Piece[] pieces, int[] rows, string label, int maxTries = 20)
    {
        if (pieces == null || pieces.Length == 0) return;

        for (int t = 0; t < maxTries; t++)
        {
            ShuffleIntoRows(pieces, rows);

            if (AllUniqueXY(pieces, out int ci, out int cj, out Piece a, out Piece b))
                return; // her taş farklı karede → tamam

            Debug.LogWarning($"[{label}] Aynı kareye iki taş geldi: ({ci},{cj})  -> {a?.name} & {b?.name}. Yeniden karıştırılıyor...");
        }

        Debug.LogError($"[{label}] maxTries sonrasında da benzersiz dağıtım sağlanamadı. Inspector listelerini ve satırları kontrol et!");
    }

    private bool AllUniqueXY(Piece[] pieces, out int clashI, out int clashJ, out Piece first, out Piece second)
    {
        clashI = clashJ = -1; first = second = null;
        var seen = new Dictionary<int, Piece>(); // key = i*10 + j

        foreach (var p in pieces)
        {
            if (p == null) continue;
            int key = p.x * 10 + p.y;
            if (seen.TryGetValue(key, out var other))
            {
                clashI = p.x; clashJ = p.y;
                first = other; second = p;
                return false;
            }
            seen[key] = p;
        }
        return true;
    }

    private void ShuffleIntoRows(Piece[] pieces, int[] rows)
    {
        var any = pieces[0];
        var slots = new List<Slot>(8);

        foreach (int row in rows)
        {
            for (int j = 0; j < ColCount; j++)
            {
                Vector2 world = any.GetGridPosition(row, j);
                slots.Add(new Slot { i = row, j = j, pos = world });
            }
        }

        // Fisher–Yates shuffle
        for (int k = slots.Count - 1; k > 0; k--)
        {
            int r = Random.Range(0, k + 1);
            (slots[k], slots[r]) = (slots[r], slots[k]);
        }

        int count = Mathf.Min(pieces.Length, slots.Count);
        for (int idx = 0; idx < count; idx++)
        {
            var p = pieces[idx];
            var s = slots[idx];

            if (p == null) continue;
            p.x = s.i;
            p.y = s.j;
            p.MoveTo(s.pos);
        }
    }

    private void RebuildGrid()
    {
        for (int i = 0; i < RowCount; i++)
            for (int j = 0; j < ColCount; j++)
                grid[i, j] = null;

        void Fill(Piece[] arr, string label)
        {
            if (arr == null) return;
            foreach (var p in arr)
            {
                if (p == null) continue;
                if (!InBounds(p.x, p.y)) continue;

                if (grid[p.x, p.y] != null)
                {
                    Debug.LogWarning($"[{label}] Aynı kareye iki taş denk geldi! ({p.x},{p.y})  -> {grid[p.x, p.y].name} & {p.name}");
                    continue;
                }
                grid[p.x, p.y] = p;
            }
        }

        Fill(myPieces, "My");
        Fill(opponentPieces, "Opp");
    }

    private void RecomputeGridOccupancy()
    {
        for (int i = 0; i < RowCount; i++)
            for (int j = 0; j < ColCount; j++)
                grid[i, j] = null;

        void Put(Piece[] arr, string label)
        {
            if (arr == null) return;
            foreach (var p in arr)
            {
                if (p == null) continue;
                if (!InBounds(p.x, p.y)) continue;

                if (grid[p.x, p.y] != null)
                {
                    Debug.LogWarning($"[Occupancy:{label}] Çakışma tespit edildi! ({p.x},{p.y}) -> {grid[p.x, p.y].name} & {p.name}");
                    continue; // ilk geleni koru
                }
                grid[p.x, p.y] = p;
            }
        }

        Put(myPieces, "My");
        Put(opponentPieces, "Opp");
    }

    private bool TryGetGridIndexFromHit(Vector3 hitPoint, out int bestI, out int bestJ)
    {
        float minDist = float.MaxValue;
        bestI = 0; bestJ = 0;

        // Referans için herhangi bir taş (GetGridPosition kullanmak için)
        Piece refPiece =
            (myPieces != null && myPieces.Length > 0 && myPieces[0] != null) ? myPieces[0] :
            (opponentPieces != null && opponentPieces.Length > 0 ? opponentPieces[0] : null);

        if (refPiece == null) return false;

        Vector2 hit2D = new Vector2(hitPoint.x, hitPoint.z);

        for (int i = 0; i < RowCount; i++)
        {
            for (int j = 0; j < ColCount; j++)
            {
                Vector2 gp = refPiece.GetGridPosition(i, j);
                float d = Vector2.Distance(hit2D, gp);
                if (d < minDist)
                {
                    minDist = d;
                    bestI = i;
                    bestJ = j;
                }
            }
        }
        return true;
    }
}
 