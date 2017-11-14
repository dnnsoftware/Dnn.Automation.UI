module DnnPages

open System.Drawing
open OpenQA.Selenium
open canopy
open CanopyExtensions
open DnnConfig
open DnnCommon

let private closeBT = "#menu-ExitEditMode>button"

/// <summary>
/// <get an element's ancient element.
/// </summary>
/// <param name="currentEl">Current element.</param>
/// <param name="generation">How many generations. </param>
/// <return> Return the ancient element. </returns>
let private getAncient(currentEl:IWebElement, generation:int) = 
    let mutable count = 0
    let mutable returnEl = currentEl
    while count < generation do
        returnEl <- returnEl.FindElement(By.XPath(".."))
        count <- count + 1
    returnEl

/// <summary>Change page path string to list.</summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return page path in list.</returns>
let private pagePath2List(pagePath:string) =
    Seq.toList (pagePath.Split '/')

/// <summary>
/// <Click the global add page button.
/// </summary>
let private clickAddPage() =
    let addPageBT = "#pages-container>div>div>div.dnn-persona-bar-page-header>div>button"
    scrollTo addPageBT
    if existsAndEnabled addPageBT then
        click addPageBT

/// <summary>
/// <Click the Add Page button while a page is selected.
/// </summary>
let private clickCurrentPageAddPageBT() =
    let addBT = "div.buttons-box>button:nth-child(2)"
    scrollTo addBT
    click (addBT)
    waitPageLoad()

/// <summary>
/// <Verify the new duplicated page, check the required button exsit or not.
/// </summary>
let private verifyDuplicatPageDisplaying() =
    let mutable returnValue = ""
    if notExists closeBT then
        waitUntilBy closeBT 2.0 |>ignore
    if notExists closeBT then
        returnValue <- returnValue + "  FAIL: Can't find Close button.\n"
    returnValue

let private detailsTab = "//ul[@class='ReactTabs__TabList']/li[1]"
let private permissionsTab = "//ul[@class='ReactTabs__TabList']/li[2]"
let private advancedTab = "//ul[@class='ReactTabs__TabList']/li[3]"

/// <summary>
/// <Switch to the specified reaction tab. 
/// </summary>
/// <param name="tabName"></param>
let switchPageTab(tabName:PageReactTabs)=
    match tabName with
        |DetailsTab -> click detailsTab
        |PermissionsTab -> click permissionsTab
        |AdvancedTab -> click advancedTab

/// <summary>
/// <Return the current enabled reaction tab name.
/// </summary>
let getCurrentPageTab()=
    if element(detailsTab).GetAttribute("aria-disabled") = "true" then
        DetailsTab
    else if element(permissionsTab).GetAttribute("aria-disabled") = "true"  then
        PermissionsTab
    else if element(advancedTab).GetAttribute("aria-disabled") = "true"  then
        AdvancedTab
    else 
        failwith "  FAIL: Unable to know the current enabled reaction tab." 

/// <summary>Find page plus button and click it.</summary>
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return true only if the button can be found. Exception will happen if the page element not exist.</returns>
let clickPlus(pagePath:string) =
    for page in pagePath2List(pagePath) do
        click ("div[id*='expand-" + page + "']")
    true

/// <summary>If a page has expand(plus) button, click it.</summary>
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
let clickPlusIfExist(pagePath:string) =
    let mutable expand = ""
    for page in pagePath2List(pagePath) do
        expand <- "div[id*='expand-" + page + "']"
        if existsAndVisible expand then
            click expand

/// <summary>Find page minus button and click it.</summary>
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return true only if the button can be found. Exception will happen if the page element not exist.</returns>
let clickMinus(pagePath:string) =
    for page in pagePath2List(pagePath) do
        click ("div[id*='collapse-" + page + "']")
    true

/// <summary>Grants all the users "View" permission on the page
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
let grantPageAllViewPerm() =
    openPagePermissions()
    let allUsersCB = "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[2]/a"
    waitForElementPresent allUsersCB
    let allUsersCBElm = element allUsersCB
    if allUsersCBElm.GetAttribute("aria-label") <> "checkbox" then
        waitClick (allUsersCB + "/*")
        if allUsersCBElm.GetAttribute("aria-label") <> "checkbox" then
            waitClick (allUsersCB + "/*")
        let saveBtn = "div.buttons-box>button[role='primary']"
        scrollTo saveBtn
        click saveBtn
        waitLoadingBar()

/// <summary>This is general way to find the save button, and click it.</summary>
let private clickSaveBT() =
    click ("//div/button[.='" + buttonSaveText + "']")

/// <summary>Find all page expand button "+" and click it.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Return the collapse button which the page being expaneded.</returns>
let expandAll() =
    let expandBT = "//div[contains(@id, 'expand-')]"
    let mutable spanId = ""
    let mutable collapseBTs = Array.empty
    while existsAndVisible (expandBT + "/*") do
        spanId <- (element (expandBT + "/../../.." )).GetAttribute("id")
        click (expandBT + "/*")
        if spanId <> "" then
            collapseBTs <- Array.append [|"//span[@id='" + spanId + "']/div/div/div/*"|] collapseBTs
    collapseBTs

/// <summary>Find the collapse button and click it.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Return true only if the button can be found.</returns>
let collapse() =
    let collapse = element "div.collapse-expand"
    let collapseExpand = collapse.Text
    if collapseExpand <> collapseText then
        false
    else
        click collapse
        true

/// <summary>Find the expand button and click it.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Return true only if the button can be found.</returns>
let expand() =
    let expand = element "div.collapse-expand"
    let collapseExpand = expand.Text
    if collapseExpand <> expandText then
        false
    else
        click expand
        true

/// <summary>
/// < Expand a page path or the whole page branch.
/// </summary>
/// <param name="pagePath">Page path.</param>
/// <param name="all"> Indicate whether it is whole page branch expandition. If true, will expand whole branch.</param>
let private expandBranch(pagePath:string, all:bool) =
    let mutable expand = element """//*[@id="pages-container"]/div/div/div[3]/div/div[1]/div/div/div[2]/div"""
    let mutable branch = expand
    let mutable expands = expand.FindElements(By.CssSelector("div[id*='expand-']"))
    for page in pagePath2List(pagePath) do
        if expand.FindElements(By.CssSelector("div[id*='expand-" + page + "']")).Count > 0 then
            expand <- expand.FindElement(By.CssSelector("div[id*='expand-" + page + "']"))
            if existsAndVisible expand then
                click expand
                expand <- branch.FindElement(By.CssSelector("div[id*='collapse-" + page + "']"))
                expand <- getAncient(expand, 5)
                branch <- expand
    if all then
        expands <- branch.FindElements(By.CssSelector("div[id*='expand-']"))
        while expands.Count > 0 do
            for i in expands do
                click i
            expands <- branch.FindElements(By.CssSelector("div[id*='expand-']"))   

/// <summary>
/// < Return pages root element.
/// </summary>
let  private getRoot()=
    element "div.scrollArea.content-horizontal>div>span.dnn-persona-bar-treeview-ul"

/// <summary>Find the page tag p element on the page tree.</summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return page element if found. Exception will happen if the page element not exist.</returns>
let private getPagePElement(pagePath:string) =
    let pagePathList = pagePath2List(pagePath)
    let mutable pageBranch = ""
    let mutable count = 0
    while count < (pagePathList.Length - 1) do
        if pageBranch <> "" then
            pageBranch <- (pageBranch + "/" + pagePathList.Item(count))
        else 
            pageBranch <- pagePathList.Item(count)
        count <- count + 1
    if pageBranch <> "" then
        clickPlusIfExist(pageBranch)
    let mutable currentBranch = getRoot()
    let mutable currentPage = currentBranch
    let mutable currentId = ""

    for pageStr in pagePathList do
        currentPage <- currentBranch |> elementWithin ("//span/p[.='" + pageStr + "']")
        currentBranch <- parent(parent(parent(parent(currentPage))))
    currentPage

/// <summary>Find the page element on the page tree.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return page element if found. Exception will happen if the page element not exist.</returns>
let findPage(pagePath:string) =
    getAncient(getPagePElement(pagePath), 3)

/// <summary>
/// < Return the page drageable element.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
let private getPageDragable(pagePath:string) =
    getAncient(getPagePElement(pagePath), 3).FindElement(By.TagName("div"))

/// <summary>
/// <Get the child pages from the page tree under pageEl, return list contains pageEl.
/// </summary>
/// <param name="pageEl">page element</param>
let private pageAndSubPages(pageEl:IWebElement)=
    let subPages = pageEl.FindElements(By.CssSelector("p"))
    let mutable returnEls = List.empty<IWebElement>
    for e in subPages do
        returnEls <- returnEls @ [e]
    returnEls

/// <summary>
/// <Get the sub pages from the page tree under pageEl.
/// </summary>
/// <param name="pageEl">page element</param>
let private getSubPages(pageEl:IWebElement)=
    let subPages = pageEl.FindElements(By.CssSelector("p"))
    let mutable returnEls = List.empty<IWebElement>
    let mutable count = 1
    while count < subPages.Count do        
        returnEls <- [subPages.Item(count)] @ returnEls
        count <- count + 1
    returnEls

/// <summary>
/// <Get the child pages(first generation) from the page tree under pageEl.
/// </summary>
/// <param name="pageEl">page element</param>
let private getChildPages(pageEl:IWebElement)=
    let subPages = pageEl.FindElements(By.XPath("./ul/li/div/div/span/p"))
    let mutable returnEls = List.empty<IWebElement>
    for p in subPages do        
        returnEls <- [p] @ returnEls
    returnEls

/// <summary>
/// <Get all first level pages.
/// </summary>
let private get1stLevelPages()=
    let root = getRoot()
    getChildPages(root)

/// <summary>
/// < Grab all '/' from the beginning of the string.
/// </summary>
/// <param name="subPath">Any string</param>
let private getHeadSlashInString(subPath:string)=
    let mutable firstChar = ""
    let mutable n = 1
    while n <= subPath.Length do
        if (subPath |> Seq.take n |> System.String.Concat) = "/" + firstChar then
            firstChar <- (firstChar + "/")
            n <- n + 1
        else 
            n <- subPath.Length + 1
    firstChar

/// <summary>
/// <Return the page elements which name match page string .
/// </summary>
/// <param name="pages">Page elements to be matched.</param>
/// <param name="page">Page name for comparing. </param>
/// <param name="branch">Branch to be expand. </param>
let rec private pagesMatch(pageEl:IWebElement, page:string, branch:string):IWebElement list =
    let mutable returnPages = List.empty<IWebElement>
    let mutable childrenPg = getChildPages(getAncient(pageEl, 4))
    if childrenPg.Length > 0 then  
        for p in childrenPg do
            if p.Text = page then
                returnPages <- returnPages @ [getAncient(p, 3)]
            expandBranch((branch + "/" + p.Text), false)
            returnPages <- returnPages @ pagesMatch(p, page, (branch + "/" + p.Text))
    returnPages

/// <summary>
/// <Return all page elements which is the last page element under the subPath. 
/// </summary>
/// <param name="subPath"> Page path to be searched, it may start from non root page. </param>
let private getSubPathPages(subPath:string) =
    let mutable returnPages: IWebElement list = List.empty<IWebElement>
    let firstChar = getHeadSlashInString(subPath)
    let mutable path = subPath
    if firstChar <> "" then
        path <- subPath.[firstChar.Length..] 
    let pagePathList = pagePath2List(path)
    if path = "" then
        returnPages <- elements "//span[contains(@class, 'dnn-persona-bar-treeview-ul tree')]/ul/li"
    else
        let mutable pages = List.empty<IWebElement>
        let mutable startPages = List.empty<IWebElement>
        if firstChar <> "" then
            pages <- get1stLevelPages()
            for p in pages do
                if p.Text = pagePathList.Item(0) then
                    expandBranch(path, false)
                    returnPages <- returnPages @ [getPagePElement(path)]
        else
            expandAll()|>ignore
            pages <- pageAndSubPages(getRoot())
            for p in pages do
                if p.Text = pagePathList.Item(0) then
                    startPages <- List.append [getAncient(p, 4)] startPages

            let mutable pageCarrier = List.empty<IWebElement>
            let mutable pageLevel = 1
            let mutable childPg = List.empty<IWebElement>
            while pageLevel < pagePathList.Length do
                for p in startPages do
                    childPg <- getChildPages p
                    for subP in childPg do
                        if subP.Text = pagePathList.Item(pageLevel) then
                            pageCarrier <- [getAncient(subP, 4)] @ pageCarrier
                startPages <- pageCarrier
                pageCarrier <- List.empty<IWebElement>
                pageLevel <- pageLevel + 1

            returnPages <- startPages
    returnPages

/// <summary>
/// <Return pages which name match page string, and it is under subPath, subPath may start from none root page.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="subPath">Page path to be searched, it may start from none root page.</param>
/// <param name="page">Page name to be compared.</param>
let getPagesInSubPath(subPath:string, page:string) =
    let paths = getSubPathPages(subPath)
    let mutable subPageEls = List.empty<IWebElement>
    let mutable returnPages = List.empty<IWebElement>
    if page <> "" then
        let firstChar = getHeadSlashInString(subPath)
        let mutable path = subPath
        if firstChar <> "" then
            path <- subPath.[firstChar.Length..]
            for e in paths do 
                returnPages <- pagesMatch(e, page, path)
        else 
            for e in paths do
                subPageEls <- getSubPages(e)
                for subPage in subPageEls do
                    if subPage.Text = page then
                        returnPages <- returnPages @ [getAncient(subPage, 3)]
    returnPages

/// <summary>Validate whether a page is shown.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return true if a page is shown. Exception will happen if any element not exist.</returns>
let isShown(pagePath:string) =
    let mutable returnValue = true
    try       
        if existsAndNotVisible( findPage(pagePath)) then
            returnValue <- false
    with
        | ex -> returnValue <- false
    returnValue

/// <summary>Validate whether the page exist.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return true if exist. Exception will happen if any element not exist.</returns>
let isExist(pagePath:string) =
    let mutable returnValue = true
    try
        findPage(pagePath)|>ignore
    with
        | Failure msg -> returnValue <- false
    returnValue

/// <summary>Validate whether a page exist and is hidden.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return true if a page exist but it is hidden. Exception will happen if any element not exist.</returns>
let isHidden(pagePath:string) =
    let mutable returnValue = isShown(pagePath)
    if not returnValue then
        let collapseBTs = expandAll()
        returnValue <- isExist(pagePath)
        for i in collapseBTs do
            click i
    else
        returnValue <-false
    returnValue

/// <summary>Get the search result list.
/// Precondition: in PB pages and has search result displaying. openPBPages() can help switch into PB pages. 
/// </summary>
/// <returns>Return search result list. Exception will happen if any element not exist.</returns>
let getPageNamesFromSearch() =
    let pages= elements("div.search-item-details-left>h1>div")
    let mutable pageTextList = List.empty<string>
    for p in pages do
        click p
        pageTextList <- pageTextList @ [p.Text]
    pageTextList

/// <summary>Search pages.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="searchText">Searching text.</param>
let searchPage(searchText:string) =
    let searchInput = "div.search-input>input"
    searchInput << searchText
    click "//div[@class='btn search-btn'][1]"

/// <summary> exit the search page and back to pages.
/// <Precondition: in search page.
/// </summary>
/// <returns>true, if processed backtopage; false, if backToPage is not exist</returns>
let leaveSearchPage() =
    let backToPage = "//*[@id='pages-container']/div/div/div[2]/div/div[1]/div"
    if existsAndVisible backToPage then
        click backToPage
        true
    else if existsAndNotVisible backToPage then
        scrollTo backToPage
        click backToPage
        true
    else
        false

/// <summary>Drag a page to be the destination page's next page, former page or child page.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <param name="pagePathTo">The destination page which the dragged page will be dropped under.</param>
/// <returns>Exception will happen if any element not exist.</returns>
///Under-design
let dragDropPage(pagePath:string, pagePathTo:string, dragType:DragType) =
    let elFrom = getPageDragable(pagePath)
    let elTo = getPageDragable(pagePathTo)

    match dragType with
    | CHILD -> javascriptDragAndDrop( elFrom, elTo)               
    | FORMER -> javascriptDragAndDrop( elFrom, elTo)
    | NEXT -> javascriptDragAndDrop( elFrom, elTo)

/// <summary>Delete a page.</summary>
/// <param name="pageEl">Page element.</param>
/// <returns>Return true. Exception will happen if any element not exist.</returns>
let private deletePage(pageEl) =
    click pageEl
    click "ul.ReactTabs__TabList>li:nth-child(1)"//click details tab
    let deleteBT = "div>div>div.buttons-box>button:nth-child(1)"

    scrollToPoint (Point(element(deleteBT).Location.X, element(deleteBT).Location.Y))
    click deleteBT//click delete button
    click "#confirmbtn"//click delete confirm button

/// <summary>Find page on the tree, open and delete it.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Return true. Exception will happen if any element not exist.</returns>
let deletePageFromTree(pagePath:string) =
    let pageEl = findPage(pagePath)
    deletePage pageEl
    true

/// <summary>page details element table.</summary>
let pageDetailsForm : PageDetailsForm =
    {
        StandardType = "//label[contains(@for, 'radio-button-Standard-normal')]"
        ExistingType = "//label[contains(@for, 'radio-button-Existing-tab')]"
        URLType = "//label[contains(@for, 'radio-button-URL-url')]"
        FileType = "//label[contains(@for, 'radio-button-File-file')]"
        Name = "//label[contains(.,'" + pageDetailNameText + "')]/../../div[2]/input"
        Title = "//label[contains(.,'" + pageDetailTitleText + "')]/../../div[2]/input"
        Description = "//label[.='" + pageDetailDescriptionText + "']/../../div[2]/textarea"
        Keywords = "div:nth-child(3)>div>div.input-tooltip-container.block>textarea"
        Tags = "div.dnn-uicommon-tags-field-input>div>div"
        TagsInput = "div.dnn-uicommon-tags-field-input>div>div>div>input"
        ParentPage = "div.dnn-page-picker>div.collapsible-label"
        DisplayInMenu = "div:nth-child(1)>div>div:nth-child(1)>div.dnn-switch-container"
        LinkTracking = " div:nth-child(2)>div>div:nth-child(1)>div>div>span"
        EnableScheduling = "div:nth-child(2)>div>div:nth-child(2)>div.dnn-switch-container>div>span"
        Workflow = "//label[.='" + pageDetailWorkflowText + "']/../../div[2]/div"

        Existingpage = "div.dnn-page-picker>div.collapsible-label"
        PermanentRedirect = "div>div.left-column>div>div>span"
        openLinkInNewWindows = "div>div.right-column>div.dnn-switch-container>div>span"

        ExternalUrl = "div.dnn-single-line-input-with-error.external-url-input>div>input"

        btBrowse = "//div[contains(@class, 'button browse')]/div/*"
        btUpload = "//div[contains(@class, 'button upload')]/div/*"
        btLink = "//div[contains(@class, 'button link')]/div/*"

        BrowseFileSystemFolder = "div.file-upload-container>div:nth-child(2)"
        BrowseFileSystemFile = "div.file-upload-container>div:nth-child(4)"

        URlLink = "div.file-upload-container>div>textarea"    }

let pagePermissionsInfo : PagePermissionForm=
   {
        AllUsersView= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[2]/a"
        AllUsersAdd= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[3]/a"
        AllUsersAddConte= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[4]/a"
        AllUsersCopy= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[5]/a"
        AllUsersDelete= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[6]/a"
        AllUsersExport= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[7]/a"
        AllUsersImport= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[8]/a"

        AllUsersManageSettin= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[9]/a"
        AllUsersNaviga= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[10]/a"
        AllUsersEdit= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[11]/a"

        RegisteredUsersView= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[2]/a"
        RegisteredUsersAdd= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[3]/a"
        RegisteredUsersAddConte= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[4]/a"
        RegisteredUsersCopy= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[5]/a"
        RegisteredUsersDelete= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[6]/a"
        RegisteredUsersExport= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[7]/a"
        RegisteredUsersImport= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[8]/a"

        RegisteredUsersManageSettin= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[9]/a"
        RegisteredUsersNaviga= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[10]/a"
        RegisteredUsersEdit= "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[11]/a"
        
   }

/// <summary>Select file.</summary>
/// <param name="fName">file name.</param>
/// <returns>Exception will happen if any element not exist.</returns>
let private selectFile(fName: string) =
    click ("//ul/li/div[contains(@class, 'item-name')][.='" + fName + "']")

/// <summary>Select file folder.</summary>
/// <param name="itemPath">item path.</param>
/// <returns>Exception will happen if any element not exist.</returns>
let private selectFromItemPicker(itemPath: string list) =
    let picker ="/"
    let mutable item = "/ul/li/div/div[contains(@class, 'item-name')][.='"
    let itemEnd = "']"
    let mutable currentI = 1
    for i in itemPath do
        if currentI = itemPath.Length then
            click (picker + item + i + itemEnd)
        else
            if notExists (picker + item + i + itemEnd+ "/../../ul/li") then
                click (picker + item + i + itemEnd + "/../../div[contains(@class, 'has-children')]")
            item <- item + i + itemEnd+ "/../.." + item
        currentI <- currentI + 1

/// <summary>Select parent or exist page from the drop down list. Used for page details Standard and existing page type.</summary>
/// <param name="parentPage">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Exception will happen if any element not exist.</returns>
let private pagePicker(parentPage: string) =
    let page = "/div[contains(@class, 'page-value')]/div[.='"
    let pageEnd = "']"
    let mutable currentLi = "//div[contains(@class, 'pages-container')]/ul/li"
    let mutable liClass:string = ""    
    let mutable currentI = 1
    let pageList = pagePath2List( parentPage )
    for i in pageList do
        if pageList.Length = 1 || i = "<< None Specified > >" then
            click (currentLi + page + i + pageEnd)
        elif currentI = pageList.Length then
            click (currentLi + "/ul/li" + page + i + pageEnd)
        else
            currentLi <- currentLi + "/ul/li" + page + i + pageEnd + "/../.."
            liClass <- (element(currentLi).GetAttribute("class"))
            currentLi <- "//li[@class='" + liClass + "']"
            if liClass.Contains("has-children closed") then
                click (currentLi + "/div[contains(@class, 'arrow-icon')]/*")
                liClass <- liClass.Replace("closed", "opened")
                currentLi <- "//li[@class='" + liClass + "']"            
        currentI <- currentI + 1

/// <summary>Pick a date time from Calendar.</summary>
/// <param name="date">Date. MM/DD/YYYY</param>
/// <returns>Exception will happen if any element not exist.</returns>
let private pickDateFromCalendar(dateTime: string, calendarName: string) =
    let dateData = Seq.toList((dateTime).Split '/')

    let dateCale = "//div[contains(@class, 'scheduler-date-row')]/div/label[.='" + calendarName + "']/../.."
    let timePicker = "/div/div[contains(@class, 'calendar-text with-time-picker')]"
    let calContainer = ""
    click (dateCale + timePicker)

    //check year first
    let yearCurrent = (element dateCale |> elementWithin "input[placeholder='YYYY']").GetAttribute("value")|> int
    let monthCurrent = ((element dateCale |> elementWithin "input[placeholder='MM']").GetAttribute("value")).TrimStart('0')|> int
    let mutable monthDif = (yearCurrent - (dateData.[2]|> int))*12 + (monthCurrent - (dateData.[0]|> int))
    if monthDif > 0 then
        while monthDif > 0 do
            click ("//span[contains(@class, 'DayPicker-NavButton--prev')]")
            monthDif <- (monthDif - 1)
    elif monthDif < 0 then
         while monthDif < 0 do
            click ("//span[contains(@class, 'DayPicker-NavButton--next')]")
            monthDif <- (monthDif + 1)

    //check day
    if dateData.[1] <> "" then
        click (element "div.calendar-container.show-below-input.visible" |> elementWithin ("//div[contains(@class, 'DayPicker-Day')][.='" + dateData.[1] + "']") )
    click ("//div[contains(@class, 'calendar-container show-below-input visible')]/button")

/// <summary>Set a date time into Calendar container.</summary>
/// <param name="dateTime">Date time. MM/DD/YYYY/hh/mm/AM(PM)</param>
/// <returns>Exception will happen if any element not exist.</returns>
let private fillDate(dateTime: string, calendarName: string) =
    let dateData = Seq.toList((dateTime).Split '/')

    let dateCale = "//div[contains(@class, 'scheduler-date-row')]/div/label[.='" + calendarName + "']/../.."
    let timePicker = "//div[contains(@class, 'calendar-text with-time-picker')]"
    scrollTo dateCale
    click (element dateCale |> elementWithin timePicker)
    if dateData.[0] <> "" then (element dateCale |> elementWithin "input[placeholder='MM']") << dateData.[0]
    if dateData.[1] <> "" then (element dateCale |> elementWithin "input[placeholder='DD']") << dateData.[1]
    if dateData.[2] <> "" then (element dateCale |> elementWithin "input[placeholder='YYYY']") << dateData.[2]
    if dateData.[3] <> "" then (element dateCale |> elementWithin "input[placeholder='hh']") << dateData.[3]
    if dateData.[4] <> "" then (element dateCale |> elementWithin "input[placeholder='mm']") << dateData.[4]

    let amPm =  element dateCale |> elementWithin "div.select-container>span"
    if amPm.Text <> dateData.[5] && ( dateData.[5] = "AM"|| dateData.[5] = "PM" ) then
        click amPm
    click ("//div[contains(@class, 'calendar-container show-below-input visible')]/button")


/// <summary>Get the "Display in Menu" setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Get the "Display in Menu" setting. Exception will happen if any element not exist.</returns>
let getDisplayInMenu() = 
    element("//label[.='Display in Menu']/../../div[2]/div/label").Text

/// <summary>Get the "Link Tracking" setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Get the "Link Tracking" setting. Exception will happen if any element not exist.</returns>
let getLinkTracking() = 
    element("//label[.='Link Tracking']/../../div[2]/div/label").Text

/// <summary>Get the "Enable Scheduling" setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Get the Enable Scheduling" setting. Exception will happen if any element not exist.</returns>
let getEnableScheduling() =
    element("//label[.='Enable Scheduling']/../../div[2]/div/label").Text

/// <summary>Get the "Permanent Redirect" setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Get the "Permanent Redirect" setting. Exception will happen if any element not exist.</returns>
let getPermanentRedirect() =
    element("//label[.='Permanent Redirect']/../../div[2]/div/label").Text

/// <summary>Get the "Open Link In New Window setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Get the "Open Link In New Window setting. Exception will happen if any element not exist.</returns>
let getOpenNewWin() =
    element("//label[.='Open Link In New Window']/../../div[2]/div/label").Text

/// <summary>Get the "Existing page" setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Get the "Existing page" setting. Exception will happen if any element not exist.</returns>
let getExistingPage() =
    element(pageDetailsForm.Existingpage).Text

/// <summary>
/// < Return the External Url setting.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
let getExternalUrl() =
    element(pageDetailsForm.ExternalUrl).GetAttribute("value")

/// <summary>Set pageType.</summary>
/// <param name="pageType">pageType.</param>
/// <returns>Exception will happen if any element not exist or the page type is not recorgnized.</returns>
let private setPageType(pageType: string) =
    match pageType.ToLower() with
    | "standard" -> click pageDetailsForm.StandardType
    | "existing" -> click pageDetailsForm.ExistingType
    | "url" -> click pageDetailsForm.URLType
    | "file" -> click pageDetailsForm.FileType
    | _ -> failwithf "  FAIL: Unrecognized page Type. Are you sure it is %A ?"  pageType

/// <summary>Get pageType.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <returns>Return the pageType string. Exception will happen if any element not exist.</returns>
let getPageType() =
    let mutable returnValue = ""
    if (element (pageDetailsForm.StandardType + "/../input")).Selected then
        returnValue <- "Standard"
    elif (element (pageDetailsForm.ExistingType + "/../input")).Selected then
        returnValue <- "Existing"
    elif (element (pageDetailsForm.URLType + "/../input")).Selected then
        returnValue <- "URL"
    elif  (element (pageDetailsForm.FileType + "/../input")).Selected then        
        returnValue <- "File"
    returnValue

/// <summary>
/// Precondition: in PB pages and a page is selected. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="infoItem"></param>
let getPageDetailInfoList(infoItem:string list) =
    let mutable returnValue = List.empty<NameValuePair>
    for f in infoItem do
        match f.ToLower() with
            | "name"  -> returnValue <- returnValue @ [{Name = f; Value = element(pageDetailsForm.Name).GetAttribute("value")}]
            | "title" -> returnValue <- returnValue @ [{Name = f; Value = element(pageDetailsForm.Title).GetAttribute("value")}]
            | "description" -> returnValue <- returnValue @ [{Name = f; Value = (element pageDetailsForm.Description).Text}]
            | "page type" -> returnValue <- returnValue @ [{Name = f; Value = getPageType()}]
            //// for pageType standard
            | "keywords" -> returnValue <- returnValue @ [{Name = f; Value = (element pageDetailsForm.Keywords).Text}]
            | "tags" ->  returnValue <- returnValue @ [{Name = f; Value = element(pageDetailsForm.Tags).GetAttribute("value")}]
            | "parent page" -> returnValue <- returnValue @ [{Name = f; Value = element(pageDetailsForm.ParentPage).Text}]
            | "display in menu" -> returnValue <- returnValue @ [{Name = f; Value = getDisplayInMenu()}]
            | "link tracking" -> returnValue <- returnValue @ [{Name = f; Value = getLinkTracking()}]
            | "enable scheduling" -> returnValue <- returnValue @ [{Name = f; Value = getEnableScheduling()}]
            | "workflow" ->  returnValue <- returnValue @ [{Name = f; Value = (element pageDetailsForm.Workflow).Text}]
            //// for pageType Existing
            | "permanent redirect" -> returnValue <- returnValue @ [{Name = f; Value = getPermanentRedirect()}]
            | "open link in new window" -> returnValue <- returnValue @ [{Name = f; Value = getOpenNewWin()}]
            | "existing page" -> returnValue <- returnValue @ [{Name = f; Value = getExistingPage()}]
            //// for pageType URL
            | "external url" -> returnValue <- returnValue @ [{Name = f; Value = getExternalUrl()}]
            | _ -> failwithf "  FAIL: Unrecognized! What detail info you want to get? Are you sure it is %A ?"  f
    returnValue

/// <summary>Fill the details form. Filling sequence is based on pageInfo list.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="parentOrExistPage">Page full tree path. EX: Activity Feed/My Profile.</param>
/// <returns>Exception will happen if any element not exist.</returns>
let fillDetails(pageInfo:NameValuePair list) =
    switchPageTab(DetailsTab)
    for f in pageInfo do
        match (f.Name).ToLower() with
            // for any pageType
            | "name"  ->  pageDetailsForm.Name << f.Value
            | "title" ->  pageDetailsForm.Title << f.Value
            | "description" -> pageDetailsForm.Description << f.Value
            // for select pageType
            | "page type" -> setPageType(f.Value)
            // for pageType standard
            | "keywords" -> pageDetailsForm.Keywords << f.Value
            | "tags" -> scrollTo pageDetailsForm.Tags
                        click pageDetailsForm.Tags
                        pageDetailsForm.TagsInput << f.Value
            | "parent page" -> click pageDetailsForm.ParentPage                                 
                               pagePicker(f.Value)
            | "display in menu" -> scrollTo pageDetailsForm.DisplayInMenu
                                   if f.Value <> getDisplayInMenu() && (f.Value = "On" || f.Value = "Off" ) then
                                        click pageDetailsForm.DisplayInMenu
            | "enable scheduling" -> scrollTo pageDetailsForm.EnableScheduling
                                     if f.Value <> getEnableScheduling() && (f.Value = "On" || f.Value = "Off" ) then
                                        click pageDetailsForm.EnableScheduling
            | "start date" -> fillDate(f.Value, "Start Date")
            | "end date" -> fillDate(f.Value, "End Date")
            // for pageType Existing
            | "permanent redirect" -> if f.Value <> getPermanentRedirect() && (f.Value = "On" || f.Value = "Off" ) then
                                        click pageDetailsForm.PermanentRedirect
            | "open link in new window" -> if f.Value <> getOpenNewWin() && (f.Value = "On" || f.Value = "Off" ) then
                                            click pageDetailsForm.openLinkInNewWindows
            | "existing page" -> click pageDetailsForm.Existingpage;
                                 pagePicker(f.Value)
            // for pageType URL
            | "external url" -> pageDetailsForm.ExternalUrl << f.Value
            // for pageType File
            | "browse button" | "browse filesystem" -> click pageDetailsForm.btBrowse
            | "Enter" | "tab Key Enter" -> press enter
            | "ESC" | "tab Key Esc" -> press esc
            | "browse filesystem folder" | "folder" -> click pageDetailsForm.BrowseFileSystemFolder;
                                                       let item = Seq.toList((f.Value).Split '/');
                                                       selectFromItemPicker(item)
            | "browse filesystem file" | "file" -> click pageDetailsForm.BrowseFileSystemFile;
                                                        selectFile(f.Value)
            | "upload button" | "upload a File" -> click pageDetailsForm.btUpload
            | "link button" |"enter url link" -> click pageDetailsForm.btLink

            | _ -> failwithf "  FAIL: Unrecognized! Which part you want to fill? Are you sure it is %A ?"  f.Name

/// <summary>Fill the page Permission form. 
/// </summary>
/// <param name="selector">The element selector to be set permission.</param>
/// <param name="check">Permission type, it can only be "unchecked" "checkbox" "denied".</param>
/// <returns>Exception will happen if any element not exist.</returns>
let private setPermission(selector:string, check:string)=
    let mutable currentStatus = (element selector).GetAttribute("aria-label")
    //"unchecked" "checkbox" "denied"
    if currentStatus <> check then
        click selector
    currentStatus <- element(selector).GetAttribute("aria-label")
    if currentStatus <> check then
        click selector
    currentStatus <- element(selector).GetAttribute("aria-label")
    if currentStatus <> check then
        failwithf "FAIL: Unrecognized permission!"


///</param>
/// </summary>
/// <param name="permissionInfo"></param>
let fillPermissions(permissionInfo:NameValuePair list) =
    switchPageTab(PermissionsTab)

    for f in permissionInfo do
        match f.Name with
            | "AllUsersView" -> setPermission(pagePermissionsInfo.AllUsersView, f.Value)
            | "AllUsersAdd" -> setPermission(pagePermissionsInfo.AllUsersAdd, f.Value)
            | "AllUsersAddConte" -> setPermission(pagePermissionsInfo.AllUsersAddConte, f.Value)
            | "AllUsersCopy" -> setPermission(pagePermissionsInfo.AllUsersCopy, f.Value)
            | "AllUsersDelete" -> setPermission(pagePermissionsInfo.AllUsersDelete, f.Value)
            | "AllUsersExport" -> setPermission(pagePermissionsInfo.AllUsersExport, f.Value)
            | "AllUsersImport" -> setPermission(pagePermissionsInfo.AllUsersImport, f.Value)

            | "AllUsersManageSettin" -> setPermission(pagePermissionsInfo.AllUsersManageSettin, f.Value)
            | "AllUsersNaviga" -> setPermission(pagePermissionsInfo.AllUsersNaviga, f.Value)
            | "AllUsersEdit" -> setPermission(pagePermissionsInfo.AllUsersEdit, f.Value)

            | "RegisteredUsersView" -> setPermission(pagePermissionsInfo.RegisteredUsersView, f.Value)
            | "RegisteredUsersAdd" -> setPermission(pagePermissionsInfo.RegisteredUsersAdd, f.Value)
            | "RegisteredUsersAddConte" -> setPermission(pagePermissionsInfo.RegisteredUsersAddConte, f.Value)
            | "RegisteredUsersCopy" -> setPermission(pagePermissionsInfo.RegisteredUsersCopy, f.Value)
            | "RegisteredUsersDelete" -> setPermission(pagePermissionsInfo.RegisteredUsersDelete, f.Value)
            | "RegisteredUsersExport" -> setPermission(pagePermissionsInfo.RegisteredUsersExport, f.Value)
            | "RegisteredUsersImport" -> setPermission(pagePermissionsInfo.RegisteredUsersImport, f.Value)

            | "RegisteredUsersManageSettin" -> setPermission(pagePermissionsInfo.RegisteredUsersManageSettin, f.Value)
            | "RegisteredUsersNaviga" -> setPermission(pagePermissionsInfo.RegisteredUsersNaviga, f.Value)
            | "RegisteredUsersEdit" -> setPermission(pagePermissionsInfo.RegisteredUsersEdit, f.Value)
            | _ -> failwithf "  FAIL: Unrecognized permission!"

///</param>
/// <returns>Return true. Exception will happen if any element not exist.</returns>
let modifyPageDetails(pagePath:string, pageInfo: NameValuePair list) =
    if pagePath <> "" then
        let page = findPage(pagePath)
        click page
    fillDetails(pageInfo)
    clickSaveBT()
    waitForAjax()
    true

/// <summary>Open context Menu while the menu button is availabe.</summary>
/// <returns>Exception will happen if the page element not exist.</returns>
let private openContextMenu() =
    click "//div[contains(@class, 'dots')]/*"

/// <summary>Validate whether it is new page.</summary>
/// <returns>Exception will happen if the page element not exist.</returns>
let  private isAnalytics()=
    let mutable returnMessage  = ""
    let title = "span#title"
    let confirm = "#confirmation-dialog>p"
    waitUntilBy title 0.5 |> ignore

    if exists title then
        if  element(title).Text <> "Page Analytics"  then
            returnMessage <- "Validate Page Analytics failed.\n "
    elif exists confirm then
        let confirmText = element(confirm).Text
        if not (confirmText.Contains "Analytics") then
            returnMessage <- "Validate Page Analytics failed.\n "

    returnMessage

/// <summary>Validate whether it is new page.</summary>
/// <returns>If passing the validation, return is empty string, else return the error message string. Exception will happen if the page element not exist.</returns>
let private isInDuplicatePage(pageType:string)=
    let mutable returnMessage  = ""

    if getPageType() <> pageType then
        returnMessage <- "PageType is wrong.\n"

    if (element pageDetailsForm.Name).GetAttribute("value") <> "" then
        returnMessage <- returnMessage + "Name should be empty.\n"

    returnMessage

/// <summary>Validate whether it is new page.</summary>
/// <returns>If passing the validation, return is empty string, else return the error message string. Exception will happen if the page element not exist.</returns>
let private isEditPage()=
    let mutable returnMessage  = ""
    let editBar = "#edit-bar"
    if notExists editBar then
        waitUntilBy editBar 0.5 |> ignore
        if notExists editBar then
            returnMessage <- "Can't find edit bar.\n "

    returnMessage

/// <summary>Validate whether it is view page mode.</summary>
/// <returns>If passing the validation, return is empty string, else return the error message string. Exception will happen if the page element not exist.</returns>
let private isViewPage()=
    let mutable returnMessage  = ""
    let btPanel = "li#Edit.btn_panel"
    if notExists(btPanel) then
        waitUntilBy btPanel  0.5 |> ignore
        if notExists(btPanel) then
            returnMessage <- "Can't find edit bar panel in view page mode.\n"

    returnMessage

/// <summary>Validate whether it is new page.</summary>
/// <param name="pagePath">The page path.</param>
/// <returns>If passing the validation, return is empty string, else return the error message string. Exception will happen if the page element not exist.</returns>
let private isNewPage(pagePath:string)=
    let mutable returnMessage  = ""
    if exists "//li[.='Localization']" then
        returnMessage <- "Add page shouldn't have Localization tab.\n "

    if exists "//ul[contains(@class, 'ReactTabs__TabList')]" then
        if element(pageDetailsForm.Name).GetAttribute("Value")  <> "" then
            returnMessage <- returnMessage + "Name should be empty.\n "

        if element(pageDetailsForm.Title).GetAttribute("Value") <> "" then
            returnMessage <- returnMessage + "Title should be empty.\n "

        if element(pageDetailsForm.Description).Text <> "" then
            returnMessage <- returnMessage + "Description should be empty.\n "

        if element(pageDetailsForm.Keywords).Text <> "" then
            returnMessage <- returnMessage + "Keywords should be empty.\n "

        let pagePathList = pagePath2List(pagePath)
        let parentPGName = pagePathList.[pagePathList.Length-1]
        if element(pageDetailsForm.ParentPage).Text <> parentPGName then
            returnMessage <- returnMessage + "ParentPage should be " + parentPGName + ".\n "
        let addPageBT = "div.dnn-persona-bar-page-header>div>button[role='primary']"
        if notExists ( addPageBT ) || (element addPageBT).GetAttribute("disabled") <> "true"  then 
            returnMessage <- returnMessage + "Missing add page button or the button is disabled.\n "
    else
        returnMessage <- returnMessage + " Can't find //ul[contains(@class, 'ReactTabs__TabList')] . \n"
    returnMessage

/// <summary>Validate whether a correct context menu item is opened.</summary>
/// <param name="pagePath">The page path.</param>
/// <param name="item">The menu item. Ex: ADDPAGE</param>
/// <param name="pageType">page type</param>
/// <returns>If passing the validation, return is empty string, else return the error message string. Exception will happen if the page element not exist.</returns>
let private validate(pagePath:string, item:PagesContextMenu, pageType:string) = 
    match item with
     | ADDPAGE -> isNewPage(pagePath)
     | VIEW -> isViewPage() 
     | EDIT -> isEditPage()
     | DUPLICATE -> isInDuplicatePage(pageType)
     | ANALYTICS -> isAnalytics()

/// <summary>Open a specified or current opened page context menu, and click the menu item if provided.
/// Precondition: in PB pages. openPBPages() can help switch into PB pages.
/// </summary>
/// <param name="pagePath">Page full path. Ex: Activity Feed/My Profile.</param>
/// <param name="item">The menu item. Ex: ADDPAGE</param>
/// <returns>Exception will happen if the page element not exist.</returns>
let clickContextMenu(pagePath:string, item:PagesContextMenu) =
    if pagePath <> "" then
        click (findPage(pagePath))
    let pageType = getPageType()
    openContextMenu()    

    let mutable itemName = ""
    match item with
     | ADDPAGE -> itemName <- pageContextMenuAddPageText
     | VIEW -> itemName <- pageContextMenuViewText 
     | EDIT -> itemName <- pageContextMenuEditText
     | DUPLICATE -> itemName <- pageContextMenuDuplicatetText
     | ANALYTICS -> itemName <- pageContextMenuAnalyticsText

    let itemIcon = sprintf "//li[contains(@class, 'dnn-in-context-menu menu-item')]/div[.='%s']/../div[contains(@class, 'icon')]" itemName
    if notExists itemIcon then
        openContextMenu()
    click itemIcon
    validate(pagePath, item, pageType)

/// <summary>
/// <Compare two namePairList.
/// </summary>
/// <param name="namePairList1">NameValuePair list to be compared. </param>
/// <param name="namePairList2">NameValuePair list to be compared.</param>
/// <returns>Return true, if match, else return false.</returns>
let compareNamePairList(namePairList1:NameValuePair list, namePairList2:NameValuePair list) =
    let mutable returnV:bool = true

    if namePairList1.Length = namePairList2.Length then
        let mutable count = 0
        while count < (namePairList1.Length) do
            if namePairList1.Item(count).Value <> namePairList2.Item(count).Value then
                returnV <- false
            count <- count + 1
    else
        returnV <- false
    returnV