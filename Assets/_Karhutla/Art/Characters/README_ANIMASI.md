# Panduan Animasi Karakter — Karhutla

Sistem animasi terpasang **otomatis** saat Play. Tidak ada scene atau prefab yang perlu diubah.

## Langsung terlihat (tanpa aset)
Pemain dan semua warga yang masih berupa kapsul mendapat **animasi prosedural**
(`ProceduralCharacterMotion`):

| Kondisi | Gerakan |
|---|---|
| Diam | bernapas (naik-turun halus, tidak serempak antar warga) |
| Jalan | memantul tiap langkah, tertekan saat menapak, bergoyang kiri-kanan |
| Lari (Shift) | pantulan lebih tinggi + condong ke depan |
| Bicara | mengangguk pelan |
| Tekan E (interaksi) | "denyut" singkat |

## Memasang model 3D
1. Taruh FBX karakter + klip animasi di `Assets/_Karhutla/Art/Characters/`.
   Klip dari Mixamo: pakai **Without Skin** untuk klip, **With Skin** untuk model.
2. Pilih FBX, buka **Rig**, set **Animation Type = Humanoid**, lalu klik Apply.
   Di tab **Animation**, centang **Loop Time** untuk idle, walk, run, dan talk.
3. Beri nama klip yang mengandung kata: `idle`, `walk`, `run`/`sprint`, `talk`, `interact`/`pickup`.
4. Menu **Karhutla > Animasi > Buat Animator Controller Karakter**.
   Klip akan terpasang otomatis, dan hasilnya disimpan di `Animators/KarhutlaCharacter.controller`.
5. Buat prefab model, lalu isi **Animator > Controller** dengan controller tadi.
   **Apply Root Motion** dibiarkan mati.
6. Menu **Karhutla > Animasi > Buat Character Model Library**, lalu isi:
   - `defaultPlayer.prefab` → model pemain
   - `defaultNpc.prefab` → model warga umum
   - `characters` → model khusus per warga. `id` = nama warga (mis. `Mas Hendra`, `Pak Tejo`)
7. Tekan Play. Kapsul otomatis diganti model, kaki diletakkan di dasar kapsul, dan animasi mengikuti gerakan.

## Parameter Animator (dibaca `CharacterAnimationDriver`)
| Parameter | Tipe | Isi |
|---|---|---|
| `Speed` | float | m/detik. Warga jalan ±1.6, pemain jalan 4.5, lari 7 |
| `IsMoving` | bool | Speed > 0.2 |
| `IsSprinting` | bool | pemain menahan Shift dan bergerak |
| `IsTalking` | bool | warga sedang dialog / pemain di dalam dialog |
| `Interact` | trigger | pemain menekan E pada objek interaktif |

Parameter yang tidak ada di controller dilewati, jadi controller custom boleh berisi sebagian saja.

## Suara langkah & event klip
Tambahkan komponen `AnimationEventRelay` di objek Animator (root model), lalu di jendela Animation tambahkan event:
- `Footstep` → memutar salah satu `footstepClips`
- `AnimEvent` dengan string, misalnya `SplashWater`. Programmer cukup berlangganan ke `OnAnimEvent`.

## File
- `Scripts/Animation/CharacterAnimationDriver.cs` — jembatan logika game → Animator
- `Scripts/Animation/ProceduralCharacterMotion.cs` — animasi cadangan untuk kapsul
- `Scripts/Animation/CharacterVisualInstaller.cs` — pemasang otomatis (termasuk warga runtime)
- `Scripts/Animation/CharacterModelLibrary.cs` — daftar model per karakter
- `Scripts/Animation/AnimationEventRelay.cs` — penerima Animation Event
- `Editor/Animation/CharacterAnimatorControllerBuilder.cs` — menu pembuat controller
- Perubahan kecil di `NpcController.cs`: properti baca-saja `IsTalking`
