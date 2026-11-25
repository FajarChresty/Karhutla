using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Pengelola Event Sinematik Storyline Terjadwal (GDD Bab 3.3, 3.4 & 8.3).
    ///
    /// Menggantikan transisi pop-up teks biasa dengan rangkaian sinematik dramatis:
    ///  1. Event Hari 40: "Skandal Izin Konsesi & Retaknya Kepercayaan" (Insiden Korupsi Lahan Gambut).
    ///     Kamera menyorot Balai Desa, widescreen letterbox, dialog konflik warga vs mandor, dan aktivasi misi investigasi.
    ///  2. Event Hari 75: "Titik Tolak Bencana: Bara di Tanah Gambut" (Kebakaran Hutan Dimulai).
    ///     Kamera menyorot hutan utara dengan efek getaran (camera shake), kepulan asap membubung, alarm kentongan,
    ///     dan transisi resmi ke Babak Survival.
    ///
    /// Tombol Debug Pengujian:
    ///  - F7: Picu langsung sinematik Hari 40 (Insiden Korupsi).
    ///  - F8: Picu langsung sinematik Hari 75 (Kebakaran Hutan).
    /// </summary>
    public class ScriptedStorylineEventManager : MonoBehaviour
    {
        public static ScriptedStorylineEventManager Instance { get; private set; }

        public class CinematicStep
        {
            public string Speaker;
            public Color SpeakerColor;
            public string Text;
            public string ObjectiveNotice;

            public CinematicStep(string speaker, Color color, string text, string objective = "")
            {
                Speaker = speaker;
                SpeakerColor = color;
                Text = text;
                ObjectiveNotice = objective;
            }
        }

        [Header("Pengaturan Sinematik")]
        [Tooltip("Izinkan event sinematik berjalan otomatis saat hari kalender tercapai.")]
        public bool autoTriggerOnDay = true;

        public bool IsCinematicPlaying { get; private set; }

        private GameObject canvasRoot;
        private RectTransform topLetterbox;
        private RectTransform bottomLetterbox;
        private CanvasGroup dialogGroup;
        private Text titleText;
        private Text subtitleText;
        private Text speakerText;
        private Text bodyText;
        private Text promptText;
        private Text objectiveNoticeText;
        private Image bannerDivider;

        private List<CinematicStep> currentSteps = new List<CinematicStep>();
        private int currentStepIndex = 0;
        private Action onCinematicFinished;

        private Transform temporaryCameraAnchor;
        private bool day40Played = false;
        private bool day75Played = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildCinematicUI();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += CheckScheduledStorylineEvents;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= CheckScheduledStorylineEvents;
            }
            if (temporaryCameraAnchor != null) Destroy(temporaryCameraAnchor.gameObject);
            if (canvasRoot != null) Destroy(canvasRoot);
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Debug Shortcut untuk review atasan / desainer
            if (Input.GetKeyDown(KeyCode.F7))
            {
                TriggerCorruptionIncidentEvent();
            }
            else if (Input.GetKeyDown(KeyCode.F8))
            {
                TriggerForestFireOutbreakEvent();
            }

            if (!IsCinematicPlaying) return;

            // Tekan E atau Spasi untuk lanjut percakapan sinematik
            if (GameInput.AdvanceDialoguePressed || GameInput.ConfirmPressed || Input.GetKeyDown(KeyCode.Return))
            {
                AdvanceStep();
            }
        }

        private void CheckScheduledStorylineEvents(int currentDay)
        {
            if (!autoTriggerOnDay) return;

            if (currentDay == 40 && !day40Played)
            {
                TriggerCorruptionIncidentEvent();
            }
            else if (currentDay == 75 && !day75Played)
            {
                TriggerForestFireOutbreakEvent();
            }
        }

        /// <summary>
        /// EVENT HARI 40: Insiden Korupsi & Skandal Lahan Gambut
        /// </summary>
        public void TriggerCorruptionIncidentEvent(Action onComplete = null)
        {
            day40Played = true;
            onCinematicFinished = onComplete;

            var steps = new List<CinematicStep>
            {
                new CinematicStep(
                    "NARASI PERISTIWA",
                    new Color(1f, 0.85f, 0.3f),
                    "Desas-desus yang berhembus beberapa pekan terakhir akhirnya meledak di Balai Desa.\n\nSalinan dokumen rahasia izin pembukaan lahan gambut lindung bocor ke tangan warga. Ratusan hektar hutan resapan air yang berbatasan langsung dengan desa telah diobral untuk dialihfungsikan.",
                    "Fase Cerita Baru: Ketegangan Agraria Memuncak"
                ),
                new CinematicStep(
                    "PAK RT BAMBANG",
                    new Color(0.4f, 0.75f, 1f),
                    "\"Bagaimana mungkin izin konsesi ini bisa keluar tanpa sosialisasi kepada warga?! Kemarau tahun ini luar biasa terik. Jika kanal-kanal itu mengeringkan air rawa gambut, tanah itu akan menjadi bubuk mesiu raksasa yang siap meledak!\"",
                    "Konflik Terbuka di Balai Desa"
                ),
                new CinematicStep(
                    "MANDOR PROYEK SAWIT",
                    new Color(1f, 0.45f, 0.45f),
                    "\"Kami hanya menjalankan surat keputusan dinas perizinan yang sah dari kabupaten. Pembersihan semak dan pembukaan kanal jalan terus demi investasi daerah. Warga jangan coba-coba menghalangi!\"",
                    "Bukti Kejahatan Lingkungan Ditemukan"
                ),
                new CinematicStep(
                    "DIARY PROTAGONIS",
                    new Color(0.95f, 0.95f, 0.95f),
                    "Suasana desa kini dicekam amarah dan kecurigaan. Hubungan antar warga mulai terbelah oleh konflik kepentingan.\n\nWarung makanku kini bukan lagi sekadar tempat singgah, melainkan pusat pertukaran informasi. Aku harus mencari bukti berkas perizinan di Balai Desa untuk membantu warga mengungkap skandal ini sebelum terlambat.",
                    "Tujuan Baru: Periksa Arsip Balai Desa & Jaga Hubungan Warga"
                )
            };

            // Titik fokus kamera ke arah Balai Desa (arsip konsesi: x=-2, z=10.5)
            Vector3 balaiDesaPos = new Vector3(-2f, 1.5f, 8.5f);
            StartCoroutine(PlayCinematicRoutine(
                "BABAK II: RETAKNYA KEPERCAYAAN",
                "Hari ke-40 — Skandal Perizinan Lahan Gambut",
                new Color(0.95f, 0.75f, 0.2f),
                balaiDesaPos,
                180f, // hadap ke Balai Desa
                28f,
                7.5f,
                steps,
                () =>
                {
                    Notifier.Show("<color=#FFE066>Objektif Investigasi Terbuka: Temukan Berkas Arsip di Balai Desa!</color>");
                    if (InvestigationSystem.Instance != null)
                    {
                        Notifier.Show("Bukti penyelidikan saat ini: " + InvestigationSystem.Instance.ClueCount + "/" + InvestigationSystem.Instance.TotalClues);
                    }
                }
            ));
        }

        /// <summary>
        /// EVENT HARI 75: Titik Tolak Bencana (Kebakaran Hutan Gambut Dimulai)
        /// </summary>
        public void TriggerForestFireOutbreakEvent(Action onComplete = null)
        {
            day75Played = true;
            onCinematicFinished = onComplete;

            var steps = new List<CinematicStep>
            {
                new CinematicStep(
                    "ALARM TANDA DARURAT",
                    new Color(1f, 0.35f, 0.2f),
                    "TENG... TENG... TENG...!\n\nSuara kentongan bertalu-talu dari pos ronda memecah keheningan desa! Udara pagi yang biasanya sejuk mendadak berubah menyengat dan berbau hangus gambut terbakar.\n\nDari batas hutan utara, kepulan asap hitam pekat membubung tinggi menelan cakrawala!",
                    "SIAGA SATU: KEBAKARAN HUTAN GAMBUT TERJADI"
                ),
                new CinematicStep(
                    "TETUA SALIM",
                    new Color(0.85f, 0.65f, 0.45f),
                    "\"Gambut di bawah tanah utara sudah terbakar... Ini bukan api semak biasa, Nak. Api menjalar di bawah lapisan tanah kering, tak terlihat mata tapi merayap cepat mendekati desa! Angin selatan akan meniup kabut asap pekat ke rumah-rumah kita!\"",
                    "Bahaya Asap Beracun & Bara Bawah Tanah"
                ),
                new CinematicStep(
                    "BU DOKTER SITI (POSKO KESEHATAN)",
                    new Color(0.4f, 0.95f, 0.7f),
                    "\"Semua warga harus segera mengenakan masker pelindung! Jangan minum sembarangan dari sumur yang tercemar abu. Anak-anak dan lansia harus berada di dalam rumah! Kita butuh pasokan air bersih dan makanan steril secepatnya!\"",
                    "Ancaman ISPA & Pencemaran Sumber Air"
                ),
                new CinematicStep(
                    "DIARY PROTAGONIS",
                    new Color(0.95f, 0.95f, 0.95f),
                    "Hari yang kutakutkan akhirnya tiba. Kehidupan damai di desa resmi berakhir.\n\nWarung makan peninggalan orang tuaku kini menjadi benteng terakhir pertahanan hidup warga. Aku harus mengamankan pasokan air bersih, memasak makanan darurat bernutrisi, dan bersama warga membangun sekat bakar di perbatasan utara sebelum desa ini habis dilalap api!",
                    "PERMAINAN BERFOKUS PENUH PADA SURVIVAL & PENYELAMATAN DESA"
                )
            };

            // Titik fokus kamera ke arah hutan utara tempat kebakaran dimulai (fireOrigin: x=0, z=48)
            Vector3 northForestPos = new Vector3(0f, 2f, 38f);
            StartCoroutine(PlayCinematicRoutine(
                "BABAK III: TITIK TOLAK BENCANA",
                "Hari ke-75 — Api Mengamuk di Hutan Gambut",
                new Color(1f, 0.35f, 0.15f),
                northForestPos,
                0f, // hadap ke Utara (tempat kebakaran)
                22f, // sudut agak mendongak melihat asap
                12f,
                steps,
                () =>
                {
                    // Pastikan api dimulai secara visual dan mekanik di DisasterSystem
                    if (DisasterSystem.Instance != null && !DisasterSystem.Instance.FireStarted)
                    {
                        // DisasterSystem akan mendeteksi hari >= 75 dan memulai api
                    }
                    Notifier.Show("<color=#FF8A7A>PERINGATAN DARURAT: Kabut asap dan bara api mulai menyelimuti desa!</color>");
                },
                withShake: true
            ));
        }

        private IEnumerator PlayCinematicRoutine(string title, string subtitle, Color themeColor,
            Vector3 cameraFocusPos, float camYaw, float camPitch, float camDist,
            List<CinematicStep> steps, Action onEndAction = null, bool withShake = false)
        {
            IsCinematicPlaying = true;
            GameState.SetInputLocked(true);

            currentSteps = steps;
            currentStepIndex = 0;

            // Setup camera focus anchor
            if (temporaryCameraAnchor == null)
            {
                var go = new GameObject("CinematicCameraAnchor");
                temporaryCameraAnchor = go.transform;
            }
            temporaryCameraAnchor.position = cameraFocusPos;

            var camFollow = CameraFollow.Instance;
            if (camFollow != null)
            {
                camFollow.SetCutsceneFocus(temporaryCameraAnchor, camYaw, camPitch, camDist, 0.6f);
            }

            // Animasikan Letterbox (Widescreen bar)
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 2.2f;
                float height = Mathf.Lerp(0f, 95f, Mathf.SmoothStep(0f, 1f, t));
                topLetterbox.sizeDelta = new Vector2(0f, height);
                bottomLetterbox.sizeDelta = new Vector2(0f, height);
                yield return null;
            }

            // Guncangan kamera saat kebakaran meletup
            if (withShake && camFollow != null)
            {
                float shakeTime = 0.8f;
                Vector3 origPos = temporaryCameraAnchor.position;
                while (shakeTime > 0f)
                {
                    shakeTime -= Time.unscaledDeltaTime;
                    temporaryCameraAnchor.position = origPos + UnityEngine.Random.insideUnitSphere * 0.45f;
                    yield return null;
                }
                temporaryCameraAnchor.position = origPos;
            }

            // Tampilkan UI Dialog Konten
            titleText.text = title;
            titleText.color = themeColor;
            subtitleText.text = subtitle;
            bannerDivider.color = themeColor;

            dialogGroup.alpha = 1f;
            dialogGroup.blocksRaycasts = true;

            UpdateStepUI();

            // Tunggu pemain membaca seluruh dialog langkah demi langkah
            while (currentStepIndex < currentSteps.Count)
            {
                yield return null;
            }

            // Selesai: tutup dialog dan kembalikan kamera ke pemain
            dialogGroup.alpha = 0f;
            dialogGroup.blocksRaycasts = false;

            if (camFollow != null)
            {
                camFollow.ClearCutsceneFocus();
            }

            // Tarik kembali letterbox
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 2.5f;
                float height = Mathf.Lerp(95f, 0f, Mathf.SmoothStep(0f, 1f, t));
                topLetterbox.sizeDelta = new Vector2(0f, height);
                bottomLetterbox.sizeDelta = new Vector2(0f, height);
                yield return null;
            }

            IsCinematicPlaying = false;
            GameState.SetInputLocked(false);

            onEndAction?.Invoke();
            onCinematicFinished?.Invoke();
            onCinematicFinished = null;
        }

        private void AdvanceStep()
        {
            currentStepIndex++;
            if (currentStepIndex < currentSteps.Count)
            {
                UpdateStepUI();
            }
        }

        private void UpdateStepUI()
        {
            if (currentStepIndex >= currentSteps.Count) return;

            var step = currentSteps[currentStepIndex];
            speakerText.text = step.Speaker;
            speakerText.color = step.SpeakerColor;
            bodyText.text = step.Text;

            if (!string.IsNullOrEmpty(step.ObjectiveNotice))
            {
                objectiveNoticeText.gameObject.SetActive(true);
                objectiveNoticeText.text = "<b>STATUS:</b> " + step.ObjectiveNotice;
            }
            else
            {
                objectiveNoticeText.gameObject.SetActive(false);
            }

            promptText.text = (currentStepIndex < currentSteps.Count - 1)
                ? "[E / Spasi] Lanjutkan Cerita (" + (currentStepIndex + 1) + "/" + currentSteps.Count + ")"
                : "[E / Spasi] Mulai Jalankan Peranmu";
        }

        private void BuildCinematicUI()
        {
            var canvas = UIFactory.Canvas;
            if (canvas == null) return;

            canvasRoot = new GameObject("ScriptedCinematicUI");
            canvasRoot.transform.SetParent(canvas.transform, false);

            var rootRt = canvasRoot.AddComponent<RectTransform>();
            UIFactory.Stretch(rootRt, 0f, 0f, 0f, 0f);

            // 1. Black Letterbox Atas & Bawah
            var topBarGo = new GameObject("TopLetterbox");
            topBarGo.transform.SetParent(rootRt, false);
            topLetterbox = topBarGo.AddComponent<RectTransform>();
            topLetterbox.anchorMin = new Vector2(0f, 1f);
            topLetterbox.anchorMax = new Vector2(1f, 1f);
            topLetterbox.pivot = new Vector2(0.5f, 1f);
            topLetterbox.sizeDelta = new Vector2(0f, 0f);
            var topImg = topBarGo.AddComponent<Image>();
            topImg.color = Color.black;
            topImg.raycastTarget = false;

            var botBarGo = new GameObject("BottomLetterbox");
            botBarGo.transform.SetParent(rootRt, false);
            bottomLetterbox = botBarGo.AddComponent<RectTransform>();
            bottomLetterbox.anchorMin = new Vector2(0f, 0f);
            bottomLetterbox.anchorMax = new Vector2(1f, 0f);
            bottomLetterbox.pivot = new Vector2(0.5f, 0f);
            bottomLetterbox.sizeDelta = new Vector2(0f, 0f);
            var botImg = botBarGo.AddComponent<Image>();
            botImg.color = Color.black;
            botImg.raycastTarget = false;

            // 2. Kotak Dialog Sinematik (Tengah Bawah)
            var dialogBox = UIFactory.CreatePanel(rootRt, "CinematicDialogBox", new Color(0.06f, 0.08f, 0.12f, 0.92f));
            UIFactory.Place(dialogBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 125f), new Vector2(980f, 280f));
            dialogGroup = dialogBox.gameObject.AddComponent<CanvasGroup>();
            dialogGroup.alpha = 0f;
            dialogGroup.blocksRaycasts = false;

            // Judul Bab
            titleText = UIFactory.CreateText(dialogBox, "TitleText", 24, TextAnchor.UpperLeft, Color.yellow);
            titleText.fontStyle = FontStyle.Bold;
            UIFactory.Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -18f), new Vector2(-50f, 32f));

            // Subtitle
            subtitleText = UIFactory.CreateText(dialogBox, "SubtitleText", 17, TextAnchor.UpperLeft, new Color(0.75f, 0.85f, 0.95f));
            UIFactory.Place(subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -48f), new Vector2(-50f, 26f));

            // Garis Pembatas
            var dividerGo = new GameObject("Divider");
            dividerGo.transform.SetParent(dialogBox, false);
            var divRt = dividerGo.AddComponent<RectTransform>();
            UIFactory.Place(divRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -76f), new Vector2(-50f, 3f));
            bannerDivider = dividerGo.AddComponent<Image>();
            bannerDivider.color = Color.yellow;

            // Nama Pembicara
            speakerText = UIFactory.CreateText(dialogBox, "SpeakerText", 20, TextAnchor.UpperLeft, Color.white);
            speakerText.fontStyle = FontStyle.Bold;
            UIFactory.Place(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -86f), new Vector2(-50f, 28f));

            // Isi Dialog / Narasi
            bodyText = UIFactory.CreateText(dialogBox, "BodyText", 18, TextAnchor.UpperLeft, Color.white);
            UIFactory.Place(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(25f, 45f), new Vector2(-50f, -165f));

            // Peringatan Objektif
            objectiveNoticeText = UIFactory.CreateText(dialogBox, "ObjectiveNoticeText", 16, TextAnchor.LowerLeft, new Color(1f, 0.85f, 0.35f));
            UIFactory.Place(objectiveNoticeText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(25f, 14f), new Vector2(-350f, 26f));

            // Petunjuk tombol Lanjut
            promptText = UIFactory.CreateText(dialogBox, "PromptText", 16, TextAnchor.LowerRight, new Color(0.7f, 0.9f, 1f));
            UIFactory.Place(promptText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-25f, 14f), new Vector2(350f, 26f));
        }
    }
}
