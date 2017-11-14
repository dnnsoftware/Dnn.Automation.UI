module DnnPageSettings

open System
open System.Drawing
open canopy

let private applyDnnCheckBox (newValue : TristateBool) panelElement =
    match newValue with
    | FALSE | TRUE -> 
        let span = panelElement |> elementWithin "span[contains(@class,'dnnCheckbox')]"
        let klass = span.GetAttribute("class")
        if newValue = TRUE && klass = "dnnCheckbox" then click span
        else 
            if newValue = FALSE && klass = "dnnCheckbox dnnCheckbox-checked" then click span
    | _ -> ()

//let private applyPermissionToCheckBox (newValue : PermissionState) (panelElement : IWebElement) =
//    match newValue with
//    | CLEAR | GRANT | DENY -> () //UNDONE
//    | _ -> ()
// tries to find an element with specific text in a pages structure tree
let scrollToMakeNodeVisible textToFind rootNode = 
    // we need to look for the node in the tree
    // the hidden nodes does not return the text of the <a> tag
    // so we need to scrolland find them
    let vetScrollBar = rootNode |> elementWithin "//div[@class='ps-scrollbar-y']"
    let mutable found = false
    let mutable maxLoops = 20
    while not found && maxLoops > 0 do
        let topLevelTabs = rootNode |> elementsWithin ".text"
        found <- topLevelTabs |> List.exists (fun e -> e.Text = textToFind)
        if not found then 
            dragElementBy vetScrollBar (Point(0, vetScrollBar.Size.Height))
            maxLoops <- maxLoops - 1

// looks recursively in page names tree to find the selected page with a given name
// no checks done if page was not found; exception will be thrown
let rec private innerSelect (parents : string List) rootElement =
    let rootNode = elementFromSelector rootElement
    let splits = rootNode.Text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
    let treeLeaves = String.Join("|", splits)
    printfn "  Selecting parent page(s): %A from top level tab(s): %A" parents treeLeaves
    match parents with
    | [] -> failwith "Parents must not be empty"
    | x :: xs -> 
        scrollToMakeNodeVisible parents.Head rootNode
        let rootNodes = rootNode |> elementsWithin ".text"

        let somePg = 
            rootNodes |> List.tryFind (fun e -> 
                                // note: we need to acces this element first and make sure it is not empty;
                                // otherwise we get element not found exception sometimes (in Chrome driver)
                                let mutable loops = 10
                                while (String.IsNullOrEmpty e.Text) && (loops > 0) do
                                    sleep 0.01
                                    loops <- loops - 1
                                x = e.Text)
        match somePg with
        | None -> failwithf "Couldn't find page %A in the list of pages" x
        | Some(pg) -> 
            // select page in tree
            hoverOver (sprintf "//a[(@class='text') and (@title='%s')]" pg.Text)
            if xs.IsEmpty then
                click pg // this click sends an AJAX request
                waitForAjax()
            else 
                let node = parent pg
                let collapsedIcons = node |> elementsWithin "a[@class='icon collapsed']"
                if collapsedIcons.IsEmpty then failwith "No sub pages under the current page to select from!"
                let icon = collapsedIcons |> List.head
                scrollTo icon
                click icon
                waitForAjax()
                innerSelect xs node

let selectFromTree (parents : string List) rootNode =
    let tree = rootNode |> elementWithin "div.dt-tree"
    if parents.Length <= 1 then
        innerSelect parents tree
    else
        let lastPg = parents |> List.rev |> List.head
        let search = rootNode |> elementWithin "input.search-input"
        scrollTo search
        search << lastPg
        let btn = rootNode |> elementWithin "a.search-button"
        click btn
        waitForAjax()
        innerSelect [lastPg] tree

/// <summary>Opens the page settings screen in PB</summary>
/// <param name="pageName">Name of the page</param>
let openPageSettings pageName = 
    openPBPages()
    "//input[@type='search']" << pageName
    let pageDiv = sprintf "//div[@class='page-item']/span/span[.='%s']/.." pageName
    sleep 0.1
    waitForAjax()
    waitForElementPresent pageDiv
    hoverOver pageDiv
    let settingsIcon = pageDiv + "/../span/span[@class='actions']/span/span[@class='settings']"
    click settingsIcon
    waitForAjax()    

let saveSettings() = click "#dnn_ctr_ManageTabs_cmdUpdate" // Update Page"

let modifyPageDetails pageUrl (details : PageSettingDetails) = 
    openPageSettings details.Name
    "//label[contains(.,'Name')]/../../div[2]/input" << details.Name
    "//label[contains(.,'Title')]/../../div[2]/input" << details.Title
    "//label[.='Description']/../../div[2]/textarea" << details.Description
    "//label[.='Keywords']/../../div[2]/textarea" << details.Keywords
    let displayInMenu = exists "span.dnn-switch-active"
    if (displayInMenu && details.IncludeInMenu = FALSE) || (not displayInMenu && details.IncludeInMenu = TRUE) then
        click "//label[.='Display in Menu']/../../div[@class='dnn-switch-container']/span/span"
    click "//div[@class='buttons-box']/button[.='Save']"
    waitForAjax()

let modifyPermissions pageName (permissions : PagePermissions) = 
    //openPageSettings pageName editPagePermissionPartialLink

    //new code
    openPageSettings pageName
    click "//li[.='Permissions']"
    waitForAjax()
    //NOT IMPLEMENTED YET

