# Praktikum 4 — A* Grid Pathfinder & Unity NavMesh Navigation

Unity 6 (6000.3.23f1) · URP · AI Navigation 2.0.14

## Cara membuka

1. Unity Hub → **Add → Add project from disk** → pilih folder `GameCerdas_Praktikum04_PathfindingNavigation`.
2. Saat project pertama kali dibuka, script `Assets/Scripts/Editor/Praktikum04SceneBuilder.cs` otomatis membuat:
   - `Assets/Scenes/P04_AStarGrid.unity` (Bagian A)
   - `Assets/Scenes/P04_NavMesh.unity` (Bagian B, NavMesh sudah di-Bake)
3. Jika scene tidak muncul: menu **Praktikum 04 → Build Semua Scene**.
   Setelah mengubah dinding di scene NavMesh: **Praktikum 04 → Bake Ulang NavMesh** (atau tombol Bake di NavMeshSurface).

## Struktur

```
Assets/Scripts/AStar/    GridNode, GridManager, AStarPathfinder, AgentPathFollower, ClickGoalSetter
Assets/Scripts/NavMesh/  NavMeshChaser, PlayerTargetMovement, NavMeshPathDebugger, DynamicObstacleMover
Assets/Scripts/Editor/   Praktikum04SceneBuilder (pembuat scene otomatis)
```

Perubahan kecil dari kode modul:
- `[DefaultExecutionOrder]` di GridManager (-100) dan AStarPathfinder (-50), supaya path sudah dihitung
  sebelum `AgentPathFollower.Start()` membacanya (di modul, urutan Start tidak dijamin).
- `AgentPathFollower` otomatis memakai path baru jika `FindPath()` dipanggil ulang (misalnya lewat klik).
- `ClickGoalSetter`: klik kiri di ground → Goal pindah, path dihitung dari posisi agent saat itu.
- `NavMeshPathDebugger`: centang **Log Status** untuk mencetak status path (bagian 43).
- `DynamicObstacleMover`: DynamicObstacle bergerak bolak-balik (Challenge 4). Matikan **Moving** untuk eksperimen statis.

## Bagian A — cara uji
Play di `P04_AStarGrid`. Warna: putih = walkable, hitam = obstacle, kuning = open set,
oranye = closed set, cyan = final path, hijau = start, merah = goal.
- Eksperimen 1: ubah GoalMarker ke (9, 0.2, 2), Play lagi (atau klik ground saat Play).
- Eksperimen 2: duplikasi cube di `Obstacles` membentuk dinding (layer harus **Obstacle**).
- Eksperimen 3: kelilingi Goal dengan obstacle → Console: "Path tidak ditemukan."

## Bagian B — cara uji
Play di `P04_NavMesh`. Gerakkan PlayerTarget dengan WASD / panah. Path NPC terlihat
(garis cyan) di Scene View dengan Gizmos aktif.
- NavMeshObstacle: pilih DynamicObstacle → bandingkan **Carve** ON vs OFF.
- NPC, PlayerTarget, dan DynamicObstacle diberi NavMeshModifier (Ignore From Build) supaya tidak ikut
  menjadi lubang saat Bake.

## Screenshot yang perlu diambil
Bagian A: grid awal, obstacle, open/closed/final path, agent mengikuti path.
Bagian B: hasil Bake NavMesh, NPC dan target, path memutari obstacle, eksperimen NavMeshObstacle.

## Jawaban Tugas Analisis (draf — sesuaikan dengan kata-katamu sendiri)

1. **Seek saja tidak cukup** karena Seek hanya mengarah lurus ke target tanpa pengetahuan tentang
   bentuk lingkungan; di depan dinding besar agent akan menabrak atau tersangkut di local minimum.
2. **Node** merepresentasikan satu cell di area game — posisi yang bisa ditempati agent beserta
   status walkable-nya.
3. **gCost** = biaya nyata yang sudah ditempuh dari start ke node tersebut (di praktikum 10 per langkah).
4. **hCost** = estimasi biaya dari node ke goal (heuristic), yang mengarahkan pencarian ke goal.
5. **fCost = g + h** menggabungkan biaya yang sudah pasti dan perkiraan sisa, sehingga A* memilih
   node yang paling menjanjikan untuk total rute terpendek, bukan hanya yang paling dekat start
   (Dijkstra) atau paling dekat goal (Greedy).
6. **Manhattan** (|dx|+|dy|) cocok untuk grid 4 arah karena itu tepat jumlah langkah minimum jika
   tidak ada obstacle, jadi tidak pernah melebihi biaya sebenarnya (admissible).
7. **parent** menyimpan node sebelumnya pada jalur terbaik, dipakai untuk menelusuri balik rute.
8. **Path dibalik** karena penelusuran parent dimulai dari goal menuju start; agent butuh urutan
   start → goal.
9. **Open set** = node yang sudah ditemukan tapi belum diproses; **closed set** = node yang sudah
   selesai diproses dan tidak perlu diperiksa lagi.
10. **Pathfinding** menghitung rute (daftar node); **path following** menggerakkan agent secara
    aktual menyusuri rute itu.
11. **NavMeshSurface** mengumpulkan geometri scene dan mem-Bake area walkable menjadi NavMesh.
12. **NavMeshAgent** meminta path di NavMesh, mengikuti corner path, dan melakukan steering serta
    local avoidance.
13. **Stopping Distance** = jarak dari destination saat agent mulai berhenti, supaya NPC tidak
    menabrak/berhimpit dengan target.
14. **Agent radius** mengikis tepi NavMesh sebesar radius; lorong yang lebih sempit dari 2× radius
    hilang dari NavMesh sehingga tidak bisa dilalui.
15. **Collider biasa** hanya berpengaruh saat Bake (menjadi geometri statis) atau tabrakan fisika;
    **NavMeshObstacle** dikenali sistem navigasi saat runtime — dihindari lewat local avoidance
    atau memotong NavMesh (carving).
16. **Carving** membuat lubang sementara pada NavMesh di area obstacle, sehingga pathfinding
    merencanakan rute baru memutarinya.
17. **NavMeshLink** diperlukan saat dua area NavMesh tidak tersambung permukaan kontinu, misalnya
    gap, lompatan, atau turun dari ledge.
18. **Repath tanpa kontrol** untuk banyak NPC setiap frame memboroskan CPU; pakai interval dan
    threshold pergerakan target (seperti di NavMeshChaser) agar path hanya dihitung ulang saat perlu.
