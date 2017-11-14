module DnnExtensions

open canopy
open System.Drawing
open System.IO

/// <summary>Changes the Section in PB Extensions</summary>
/// <param name="sectionName">The name of the section</param>
let changeExtSection sectionName =
    let sectionDdn = "div.dnn-dropdown"
    let doChangeSection()=
        try
            scrollToOrigin()
            waitLoadingBar()
            let sectionDdnInput = sectionDdn + ">input"
            click sectionDdn
            sectionDdnInput << sectionName
            let sectionSelected = sprintf "//div[contains(@class,'dropdown-tooltip-container')]/div/div/div/div/div/div/ul/li[.='%s' and @class='selected']" sectionName
            waitForElementPresent sectionSelected
            click sectionSelected
            let sectionChosen = sprintf "//div[contains(@class,'dnn-dropdown')]/div[.='%s']" sectionName
            waitForElementPresent sectionChosen
            waitForAjax()
            true
        with _ ->
            reloadPage()
            waitForElementPresent sectionDdn
            false
    let changeSucess = retryWithWait 3 0.5 doChangeSection
    if not changeSucess then
        failwithf "  FAIL: PB Section %A could not be changed to successfully." sectionName

/// <summary>Uninstalls a Host Extension. Host should be already logged in.</summary>
/// <param name="sectionName">The name of the section to which extension belong, e.g. Persona Bar, Providers etc.</param>
/// <param name="moduleName">The name of the extension</param>
let uninstallHostExtension sectionName moduleName = 
    openPBExtenstions()
    changeExtSection sectionName
    let extName = sprintf "//span[.=\"%s\"]" moduleName
    scrollTo extName
    scrollByPoint(Point(0,-200))
    let delIcon = extName + "/../../div/div[1]/*"
    let delBtn = (sprintf "//button[.=\"%s\"]" deleteText)
    click delIcon
    waitForElementPresent delBtn
    waitForAjax()
    click "div.delete-files-box>div>div.dnn-label>label"    
    click delBtn
    click "#confirmbtn"
    waitForAjax()
    waitForElementPresent (sprintf "//button[.=\"%s\"]" installExtensionAction)

/// <summary>Installs a Host Extension. Host should be already logged in.</summary>
/// <param name="path">The path of the module install zip file.</param>
/// <param name="modulename">The name of the module.</param>
let installHostExtension path modulename = 
    openPBExtenstions()
    let uploadInput = "#dropzoneId>div>div>input"
    let nextBtn = "div.modal-footer>button:nth-child(2)"     
    click installExtBtn
    if not(File.Exists(path)) then 
        failwithf "  ERROR: File does not exist: %A" path 
    waitForElementPresent nextBtn
    waitForAjax()
    uploadInput << (fixfilePath path)
    waitForAjax()
    if existsAndVisible "div.already-installed-container>p.repair-or-install" then
        failwithf "  ERROR: Module %A is already installed." modulename
    waitForElementPresent "//div[@class='upload-percent' and .='100%']"    
    click nextBtn
    scrollTo nextBtn //long screen
    click nextBtn
    click nextBtn
    waitForAjax()
    let acceptCbox = "div.checkbox>label"
    click acceptCbox
    waitForAjax()
    click nextBtn
    waitForAjax()
    //Done button
    click "div.modal-footer>button"
    waitForAjax()
    waitForElementPresent installExtBtn

/// <summary>Checks if the module image exists</summary>
/// <param name="imgname">The name of the module image</param>
/// <param name="modulename">The name of the module</param>
/// <returns>True if the module image exists, false otherwise.</returns>
let checkModuleImageExists imgname modulename =
    let pageLink = 
        match imgname with
        | "SQL" | "Dashboard" | "Configuration Manager" -> "/Host"
        | _ -> "/Admin"
    goto pageLink
    let element3 = sprintf "//div[contains(@title,'%s')]" imgname
    existsAndVisible element3

/// <summary>Verifies module usage in Extensions</summary>
/// <param name="modulename">The name of the module</param>
let verifyModuleUsage moduleName =
    openPBExtenstions()
    let inUse = sprintf"//span[.='%s']/../../div[4]/div[@class='in-use']" moduleName
    let notinUse = sprintf"//span[.='%s']/../../div[4]/p[.='No']" moduleName
    if existsAndVisible inUse then
        click inUse
        let moduleUsagePopUp = sprintf"//div[@class='modepanel-content-title' and .='Module Usage for %s']" moduleName
        if not (existsAndVisible moduleUsagePopUp) then
            failwithf " Module usage information is not available for %s" moduleName
        click "//button[@class='dnn-ui-common-button small']"
    else
        if not (existsAndVisible notinUse) then
            failwithf " Module is not in Use is not visible for %s" moduleName

