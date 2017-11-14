module DnnRecycleBin

open canopy

/// <summary>Checks if a page is in Recycle Bin</summary>
/// <param name="pageName">Name of the page</param>
/// <returns>True if page is in recycle bin, false otherwise</returns>
let checkPageInRecycleBin pageName =
    let pageDiv = sprintf "//div[@class='pagename' and .=%A]" pageName
    existsAndVisible pageDiv

/// <summary>
/// Deletes a page from Pages section
/// </summary>
/// <param name="pageName">Name of the page</param>
let deletePage pageName = 
    let pageItem = sprintf "//div[contains(@id,\"title-%s-\")]" pageName
    waitClick pageItem
    waitLoadingBar()
    let deleteBtn = "div.buttons-box>button:first-of-type[role='secondary']"
    if not(exists deleteBtn) then waitForElement deleteBtn
    scrollTo deleteBtn
    click deleteBtn
    waitClick "a#confirmbtn"
    waitLoadingBar()
    waitPageLoad()    

/// <summary>
/// Performs an action on a page in Recycle Bin
/// </summary>
/// <param name="pageName">Name of the page</param>
/// <param name="actionName">Action to perform - restore, delete</param>
let private actOnPageRB pageName actionName =
    openPBRecycleBin()
    click "//li/a[@href='#pages']" //Pages tab
    waitForAjax()
    let pageDiv = sprintf "//div[.='%s']" pageName
    hoverOver pageDiv
    let restoreIcon = pageDiv + sprintf "/../../../../../../../../td[contains(@class,'actions')]/span[@class='%s']" actionName
    waitForElementPresent restoreIcon
    click restoreIcon
    let confirmBtn = "div#confirmation-dialog>div>a#confirmbtn"
    waitForElementPresent confirmBtn
    click confirmBtn
    waitForAjax()
    if existsAndVisible pageDiv then
        failwithf "  FAIL: Page %A is still visible after %s" pageName actionName

/// <summary>Removes a page from Recycle Bin</summary>
/// <param name="pageName">Name of the page</param>
let removePage pageName = 
    actOnPageRB pageName "remove"

/// <summary>Restores a page from Recycle Bin</summary>
/// <param name="pageName">Name of the page</param>
let restorePage pageName = 
    actOnPageRB pageName "restore"

/// <summary>Empties the Recycle Bin</summary>
let emptyRecycleBin()=
    openPBRecycleBin()
    click "button.emtyRecycleBin"
    let confirmBtn = "div#confirmation-dialog>div>a#confirmbtn"
    waitForElementPresent confirmBtn
    click confirmBtn
    waitForAjax()
    waitLoadingBar()
