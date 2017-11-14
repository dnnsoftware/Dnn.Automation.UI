[<AutoOpen>]
module CanopyExtensions

//overwrite and extend canopy functions here
open System
open System.Drawing
open System.IO
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open canopy

let backspace = Keys.Backspace

let getBrowserWait timeoutSec = WebDriverWait(browser, TimeSpan.FromSeconds(timeoutSec))

let execJs script el =
    (browser :?> IJavaScriptExecutor).ExecuteScript(script, element)

let javascriptDragAndDrop(elFrom:IWebElement, elTo:IWebElement)=
    let javaScriptPath = Path.Combine(exeLocation, "..\javascript\drag_and_drop_helper.js")
    let mutable script:string = File.ReadAllText(javaScriptPath)
    script <- script + "simulateHTML5DragAndDrop(arguments[0], arguments[1])"
    (browser :?> IJavaScriptExecutor).ExecuteScript(script, elFrom, elTo)

let elementFromSelector selector =
    match box selector with
    | :? IWebElement as el -> el
    | :? String as s -> element s
    | _ -> failwith "Unknown selector type"

let textFromSelector selector =
    match box selector with
    | :? String as s -> s
    | :? IWebElement as e -> e.Text
    | _ -> selector.ToString()

let disabled cssSelector = 
    waitFor (fun _ -> (element cssSelector).GetAttribute("class").Contains("buttonDisabled"))
    let currentUrl = currentUrl()
    click cssSelector
    on currentUrl

let enabled cssSelector = waitFor (fun _ -> not ((element cssSelector).GetAttribute("class").Contains("buttonDisabled")))
let red cssSelector = waitFor (fun _ -> (element cssSelector).GetAttribute("style").Contains("border-color: red"))
let clickAll items = items |> List.iter click

let private selectorsOf cssSelector tag = 
    let rec lookforInputs (p : IWebElement) = 
        let children = p.FindElements(By.TagName(tag)) |> Seq.toList
        match children with
        | [] -> 
            if p.TagName = "body" then []
            else lookforInputs (parent p)
        | _ -> children

    let label = unreliableElement cssSelector
    if isNull label then []
    else 
        let p = parent label
        lookforInputs p

let private selectorOf cssSelector tag = 
    let children = selectorsOf cssSelector tag
    match children with
    | [] -> None
    | x :: _ -> Some(x) // or should we throw excepion for more than one element?

// the following functions select within all document scope
let inputOf cssSelector = selectorOf cssSelector "input"
let inputsOf cssSelector = selectorsOf cssSelector "input"

let textBoxOf cssSelector = 
    let foundElements = inputsOf cssSelector
    foundElements
    |> List.filter (fun e -> 
           let t = e.GetAttribute("type")
           t = "text" || t = "password" || t = "textarea")
    |> List.head

let checkBoxOf cssSelector = 
    let foundElements = inputsOf cssSelector
    foundElements
    |> List.filter (fun e -> 
           let t = e.GetAttribute("type")
           t = "checkbox")
    |> List.head

let radioButtonsOf cssSelector = inputsOf cssSelector |> List.filter (fun e -> e.GetAttribute("type") = "radio")

let clickCboxLabel cboxId = 
    let label = sprintf "//label[(@for='%s')]" cboxId
    click label

let clickCboxImage selector = 
    let someE =
        match box selector with
        | :? IWebElement as e -> Some(e)
        | :? String as s -> someElement s
        | _ -> None
    match someE with
    | Some(e) ->
        let prnt = e |> parent
        let cboxImg = prnt |> elementWithin "img"
        click cboxImg
    | None -> ()

// clicks the nth radio button (xx first click a 1st or 2nd button to guarantee selection xx not valid anymore)
// must have at least nth element or fails with exception
// nth value passed assumes 1st element index = 1
let clickNthRadioButton cssSelector n = 
    let clickBtnImg (checkBox : IWebElement) =
        clickCboxImage ("#" + checkBox.GetAttribute("id"))

    let radioBtns = radioButtonsOf cssSelector
    // radio buttons behaving as toggles sometimes; this is a work-around
    //match n with
    //| 1 -> clickBtnImg radioBtns.Tail.Head  // click the 2nd element then the required one
    //| _ -> clickBtnImg radioBtns.Head       // click the 1st element then the required one
    //sleep 0.1
    clickBtnImg radioBtns.[n - 1] // index starts @ 0

let clickExpandLink selector =
    click selector
    sleep 0.5

// finds a text input element, then appends to its value another text
let (<<<) cssSelector text = 
    let textbox = element cssSelector
    let original = read textbox
    textbox << (original + text)

let appentToTextBox cssSelector text = cssSelector <<< text
// an alias for the ( << ) operator
let write text cssSelector = cssSelector << text

let exists selector = 
    let someE = someElement selector
    match someE with
    | Some(_) -> true
    | None -> false

let notExists selector = not (exists selector)

let private isVisible selector = 
    let someE = 
        match box selector with
        | :? IWebElement as e -> Some(e)
        | :? String as s -> someElement s
        | _ -> None
    match someE with
    | Some(e) -> 
        try 
            let opacity = e.GetCssValue("opacity")
            let display = e.GetCssValue("display")
            let r = display <> "none" && opacity = "1" && e.Displayed
            //if not r && Diagnostics.Debugger.IsAttached then 
            //    printfn "  existsAndVisible ( %A ) => display: %A, opacity: %A, displayed: %A, enabled: %A"
            //        selector display opacity e.Displayed e.Enabled
            r
        with ex -> 
            printfn "  %s" ex.Message
            false // if the item is not found and went out of scope; we assume it is not visible
    | None -> false

let private isPartiallyVisible selector = 
    let someE = 
        match box selector with
        | :? IWebElement as e -> Some(e)
        | :? String as s -> someElement s
        | _ -> None
    match someE with
    | Some(e) -> 
        try 
            let opacity = e.GetCssValue("opacity")
            let display = e.GetCssValue("display")
            let r = display <> "none" && opacity <> "0" && e.Displayed
            //if not r && Diagnostics.Debugger.IsAttached then 
            //    printfn "  existsAndVisible ( %A ) => display: %A, opacity: %A, displayed: %A, enabled: %A"
            //        selector display opacity e.Displayed e.Enabled
            r
        with ex -> 
            printfn "  %s" ex.Message
            false // if the item is not found and went out of scope; we assume it is not visible
    | None -> false

let existsAndVisible selector = 
    match box selector with
    | :? IWebElement as e -> isVisible e
    | :? String as s -> isVisible s
    | _ -> failwith "passed selector is not element or string"

let existsAndPartiallyVisible selector = 
    match box selector with
    | :? IWebElement as e -> isPartiallyVisible e
    | :? String as s -> isPartiallyVisible s
    | _ -> failwith "passed selector is not element or string"

let existsAndEnabled selector = 
    match box selector with
    | :? IWebElement as e -> e.Enabled
    | :? String as s ->  exists s && (element s).Enabled
    | _ -> failwith "passed selector is not element or string"

let existsAndNotVisible selector = 
    match box selector with
    | :? IWebElement as e -> not (isVisible e)
    | :? String as s -> exists s && not (isVisible s)
    | _ -> failwith "passed selector is not element or string"

let scrollByPoint (p : Point) = 
    let script = sprintf "window.scrollBy(%d,%d)" p.X p.Y
    js script |> ignore
    sleep 0.1

let scrollToPoint (p : Point) = 
    let script = sprintf "window.scrollTo(%d,%d)" p.X p.Y
    js script |> ignore
    sleep 0.1

let scrollToOrigin() = 
    let origin = Point(0, 0)
    scrollToPoint origin

let scrollTo selector =
    let element =
        match box selector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed selector is not element or string"
    let point = Point( element.Location.X, element.Location.Y - element.Size.Height - 50 )
    scrollToPoint point
    sleep 0.2

/// <summary>Scroll to a point above an element</summary>
/// <param name="selector">Selector of the element to scroll to</param>
/// <param name="pixels">Number of pixels to scroll to above the element</param>
let scrollToAboveElement selector pixels =
    let element =
        match box selector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed selector is not element or string"
    let point = Point( element.Location.X, element.Location.Y - element.Size.Height - pixels )
    scrollToPoint point
    sleep 0.2

let moveCursorTo selector = 
    let element =
        match box selector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed selector is not element or string"
    let p = element.Location
    let actions = Actions(browser)
    actions.MoveToElement(element).Perform()

let scrollElementIntoView selector = 
    let element =
        match box selector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed selector is not element or string"
    if not element.Displayed then 
        execJs "arguments[0].scrollIntoView(true);" element |> ignore
        sleep 0.5

let dragElementBy selector (delta : Point) = 
    let element =
        match box selector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed selector is not element or string"
    let actions = Actions(browser)
    actions.DragAndDropToOffset(element, delta.X, delta.Y).Perform()

let dragAndDrop dragselector destinationselector =
    printfn "  Drag and Drop %A into %A" dragselector destinationselector
    let dragelement =
        match box dragselector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed drag selector is not element or string"
    let destinationelement =
        match box destinationselector with
        | :? IWebElement as e -> e
        | :? String as s -> element s
        | _ -> failwith "passed destination selector is not element or string"
    let actions = Actions(browser)
    actions.DragAndDrop(dragelement, destinationelement).Perform()
    sleep 1

let rec loopUntilVisible retries (e : IWebElement) = 
    let opacity = e.GetCssValue("opacity")
    let display = e.GetCssValue("display")
    let r = display <> "none" && opacity = "1" && e.Displayed
    if not r then 
        match retries with
        | 0 -> ()
        | _ -> 
            sleep 0.5
            loopUntilVisible (retries - 1) e

let hoverOver cssSelector = 
    match box cssSelector with
    | :? IWebElement as e ->
        let actions = Actions(browser)
        actions.MoveToElement(e).Perform()
        moveCursorTo e
        loopUntilVisible 10 e
    | :? String as s ->
        hover s
        moveCursorTo s
        let e = element s
        loopUntilVisible 10 e
    | _ -> failwith "passed selector is not element or string"

// sends SHIFT+KEY to browser
let sendShiftPlusKey key  =
    let actions = Actions(browser)
    actions.KeyDown(Keys.Shift).SendKeys(key).KeyUp(Keys.Shift).Perform() |> ignore

// sends CTRL+KEY to browser
let sendCtrlPlusKey key  =
    let actions = Actions(browser)
    actions.KeyDown(Keys.Control).SendKeys(key).KeyUp(Keys.Control).Perform() |> ignore

// sends ALT+KEY to browser
let sendAltPlusKey key  =
    let actions = Actions(browser)
    actions.KeyDown(Keys.Alt).SendKeys(key).KeyUp(Keys.Alt).Perform() |> ignore

let switchToWindow window =
    browser.SwitchTo().Window(window) |> ignore

let getOtherWindow currentWindow =
    browser.WindowHandles |> Seq.find (fun w -> w <> currentWindow)

let switchToOtherWindow currentWindow =
    switchToWindow (getOtherWindow currentWindow) |> ignore

let closeOtherWindow currentWindow =
    switchToOtherWindow currentWindow
    browser.Close()
