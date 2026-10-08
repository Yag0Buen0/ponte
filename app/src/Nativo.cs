// Chamadas da API do Windows usadas pelo modo jogo (hooks de teclado/mouse e cursor).
using System;
using System.Runtime.InteropServices;

namespace CelularRemoto
{
    static class Nativo
    {
        public const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
        public const int WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202,
                         WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205, WM_MBUTTONDOWN = 0x207, WM_MBUTTONUP = 0x208,
                         WM_MOUSEWHEEL = 0x20A;
        public const uint LLKHF_INJECTED = 0x10, LLMHF_INJECTED = 0x01;

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct PONTO { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Esquerda, Topo, Direita, Base; }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr extra; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT { public PONTO pt; public uint mouseData, flags, time; public IntPtr extra; }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc proc, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string nome);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr janela);
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr janela, out RECT r);
        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr janela, ref PONTO p);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr janela);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);
        // Coordenadas fisicas (independem da escala de DPI): as mesmas dos hooks de baixo nivel
        [DllImport("user32.dll")]
        public static extern bool GetPhysicalCursorPos(out PONTO p);
        [DllImport("user32.dll")]
        public static extern bool ClipCursor(ref RECT r);
        [DllImport("user32.dll", EntryPoint = "ClipCursor")]
        public static extern bool LiberarCursor(IntPtr nulo);

        // Area cliente (onde a imagem do celular aparece) em coordenadas de tela
        public static System.Drawing.Rectangle AreaCliente(IntPtr janela)
        {
            RECT r;
            GetClientRect(janela, out r);
            var p = new PONTO();
            ClientToScreen(janela, ref p);
            return new System.Drawing.Rectangle(p.X, p.Y, r.Direita - r.Esquerda, r.Base - r.Topo);
        }
    }
}
