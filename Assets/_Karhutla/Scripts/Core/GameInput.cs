using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Karhutla
{
    /// <summary>
    /// Pembungkus input. Semua script membaca input lewat class ini, jadi hanya di sini
    /// yang perlu diubah kalau ingin pindah ke Input Actions.
    /// Mendukung Input Manager lama (UnityEngine.Input) dan Input System baru secara otomatis.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        private static bool Down(Key k) { var kb = Keyboard.current; return kb != null && kb[k].wasPressedThisFrame; }
        private static bool Held(Key k) { var kb = Keyboard.current; return kb != null && kb[k].isPressed; }

        public static Vector2 Move
        {
            get
            {
                float x = (Held(Key.D) || Held(Key.RightArrow) ? 1f : 0f) - (Held(Key.A) || Held(Key.LeftArrow) ? 1f : 0f);
                float y = (Held(Key.W) || Held(Key.UpArrow) ? 1f : 0f) - (Held(Key.S) || Held(Key.DownArrow) ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }

        public static bool SprintHeld => Held(Key.LeftShift);
        public static bool InteractPressed => Down(Key.E);
        public static bool AdvanceDialoguePressed => Down(Key.E) || Down(Key.Space);
        public static bool InventoryTogglePressed => Down(Key.I) || Down(Key.Tab);
        public static bool SecondaryInteractPressed => Down(Key.Q);
        public static bool ConfirmPressed => Down(Key.E);
        public static bool CancelPressed => Down(Key.Escape) || Down(Key.Q);

        // Tahap 2: kamera 4 arah, aksi ketiga, menu penyimpanan/barter, panel info.
        public static bool CameraLeftPressed => Down(Key.Z);
        public static bool CameraRightPressed => Down(Key.C);
        public static bool TertiaryInteractPressed => Down(Key.R);
        public static bool SwitchModePressed => Down(Key.R);
        public static bool DiscardPressed => Down(Key.G);
        public static bool ClearPressed => Down(Key.Backspace);
        public static bool RelationshipTogglePressed => Down(Key.K);
        public static bool RelationshipTabPressed => Down(Key.Tab) || Down(Key.T);
        public static bool ReportPressed => Down(Key.L);

        // Kamera: scroll untuk zoom, tombol mouse kanan/tengah ditahan + geser untuk memutar bebas.
        public static float ScrollDelta
        {
            get
            {
                var m = Mouse.current;
                if (m == null) return 0f;
                float y = m.scroll.ReadValue().y;
                return Mathf.Abs(y) >= 10f ? y / 120f : y; // Input System melaporkan +-120 per notch di sebagian platform
            }
        }

        public static bool CameraDragHeld
        {
            get { var m = Mouse.current; return m != null && (m.rightButton.isPressed || m.middleButton.isPressed); }
        }

        public static Vector2 MouseDelta
        {
            get { var m = Mouse.current; return m != null ? m.delta.ReadValue() : Vector2.zero; }
        }

        // Tombol DEBUG (hanya untuk pengujian, bukan mekanik game).
        public static bool DebugFirePressed => Down(Key.F);
        public static bool DebugTimeUnitPressed => Down(Key.T);
        public static bool DebugNextSectionPressed => Down(Key.Y);
        public static bool DebugNextDayPressed => Down(Key.U);
        public static bool DebugPlotSkipPressed => Down(Key.P);

        // Barter: ubah jumlah barang yang diminta. Debug hubungan sosial: ] naik, [ turun, \ reset semua.
        public static bool MenuUpPressed => Down(Key.UpArrow);
        public static bool MenuDownPressed => Down(Key.DownArrow);
        public static bool DebugRelationUpPressed => Down(Key.RightBracket);
        public static bool DebugRelationDownPressed => Down(Key.LeftBracket);
        public static bool DebugRelationResetPressed => Down(Key.Backslash);

        // Aksi keempat (hadiah), kunci kamera mouse, simpan/muat.
        public static bool QuaternaryInteractPressed => Down(Key.H);
        public static bool CameraLockTogglePressed => Down(Key.M);
        public static bool SaveGamePressed => Down(Key.F5);
        public static bool LoadGamePressed => Down(Key.F9);
        public static bool NewGamePressed => Down(Key.F10);
        public static bool LeftClickPressed { get { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; } }
        /// <summary>Gerak mouse/touchpad frame ini, kira-kira dalam piksel (skala sama di kedua backend input).</summary>
        public static Vector2 LookDelta { get { var m = Mouse.current; return m != null ? m.delta.ReadValue() : Vector2.zero; } }

        public static bool NumberPressed(int n)
        {
            if (n < 1 || n > 9) return false;
            return Down((Key)((int)Key.Digit1 + (n - 1)));
        }
#else
        public static Vector2 Move => Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        public static bool SprintHeld => Input.GetKey(KeyCode.LeftShift);
        public static bool InteractPressed => Input.GetKeyDown(KeyCode.E);
        public static bool AdvanceDialoguePressed => Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
        public static bool InventoryTogglePressed => Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
        public static bool SecondaryInteractPressed => Input.GetKeyDown(KeyCode.Q);
        public static bool ConfirmPressed => Input.GetKeyDown(KeyCode.E);
        public static bool CancelPressed => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q);

        // Tahap 2: kamera 4 arah, aksi ketiga, menu penyimpanan/barter, panel info.
        public static bool CameraLeftPressed => Input.GetKeyDown(KeyCode.Z);
        public static bool CameraRightPressed => Input.GetKeyDown(KeyCode.C);
        public static bool TertiaryInteractPressed => Input.GetKeyDown(KeyCode.R);
        public static bool SwitchModePressed => Input.GetKeyDown(KeyCode.R);
        public static bool DiscardPressed => Input.GetKeyDown(KeyCode.G);
        public static bool ClearPressed => Input.GetKeyDown(KeyCode.Backspace);
        public static bool RelationshipTogglePressed => Input.GetKeyDown(KeyCode.K);
        public static bool RelationshipTabPressed => Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.T);
        public static bool ReportPressed => Input.GetKeyDown(KeyCode.L);

        // Kamera: scroll untuk zoom, tombol mouse kanan/tengah ditahan + geser untuk memutar bebas.
        public static float ScrollDelta => Input.mouseScrollDelta.y;
        public static bool CameraDragHeld => Input.GetMouseButton(1) || Input.GetMouseButton(2);
        public static Vector2 MouseDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Tombol DEBUG (hanya untuk pengujian, bukan mekanik game).
        public static bool DebugFirePressed => Input.GetKeyDown(KeyCode.F);
        public static bool DebugTimeUnitPressed => Input.GetKeyDown(KeyCode.T);
        public static bool DebugNextSectionPressed => Input.GetKeyDown(KeyCode.Y);
        public static bool DebugNextDayPressed => Input.GetKeyDown(KeyCode.U);
        public static bool DebugPlotSkipPressed => Input.GetKeyDown(KeyCode.P);

        // Barter: ubah jumlah barang yang diminta. Debug hubungan sosial: ] naik, [ turun, \ reset semua.
        public static bool MenuUpPressed => Input.GetKeyDown(KeyCode.UpArrow);
        public static bool MenuDownPressed => Input.GetKeyDown(KeyCode.DownArrow);
        public static bool DebugRelationUpPressed => Input.GetKeyDown(KeyCode.RightBracket);
        public static bool DebugRelationDownPressed => Input.GetKeyDown(KeyCode.LeftBracket);
        public static bool DebugRelationResetPressed => Input.GetKeyDown(KeyCode.Backslash);

        // Aksi keempat (hadiah), kunci kamera mouse, simpan/muat.
        public static bool QuaternaryInteractPressed => Input.GetKeyDown(KeyCode.H);
        public static bool CameraLockTogglePressed => Input.GetKeyDown(KeyCode.M);
        public static bool SaveGamePressed => Input.GetKeyDown(KeyCode.F5);
        public static bool LoadGamePressed => Input.GetKeyDown(KeyCode.F9);
        public static bool NewGamePressed => Input.GetKeyDown(KeyCode.F10);
        public static bool LeftClickPressed => Input.GetMouseButtonDown(0);
        /// <summary>Gerak mouse/touchpad frame ini, kira-kira dalam piksel (skala sama di kedua backend input).</summary>
        public static Vector2 LookDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;

        public static bool NumberPressed(int n)
        {
            if (n < 1 || n > 9) return false;
            return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + (n - 1)));
        }
#endif
    }
}
