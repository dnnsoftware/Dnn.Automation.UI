// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

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

