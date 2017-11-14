module InputSimulatorHelper

open System.IO
open System.Reflection
open WindowsInput.Native
open canopy

/// <summary>Press CTRL+SHIFT+KEY.</summary>
/// <param name="key">The third key.</param>
let private pressCtrlShiftPlusKey(key:VirtualKeyCode) =
    let simulator = WindowsInput.InputSimulator()
    let keys =  [|VirtualKeyCode.CONTROL; VirtualKeyCode.SHIFT|]
    simulator.Keyboard.ModifiedKeyStroke(keys, key )

/// <summary>Press CTRL+KEY.</summary>
/// <param name="key">The key.</param>
let private pressCtrlPlusKey(key:VirtualKeyCode) =
    let simulator = WindowsInput.InputSimulator()
    simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, key )

/// <summary>Press KEY.</summary>
/// <param name="key">The key.</param>
let private pressKey(key:VirtualKeyCode) =
    let simulator = WindowsInput.InputSimulator()
    simulator.Keyboard.KeyPress(key)

/// <summary>Hard reloads a page by sending Ctrl+F5 directly to window in focus</summary>
let hardReloadPage() =
    pressCtrlPlusKey VirtualKeyCode.F5 |> ignore

/// <summary>Presses the F11 key. This will enter or exit fullscreen mode.</summary>
let pressF11() =
    pressKey(VirtualKeyCode.F11) |> ignore

