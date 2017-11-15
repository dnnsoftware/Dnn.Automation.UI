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

module DnnHtmlModule

open System
open canopy
open DnnHost

/// <summary>Test case helper to verify text in Html and Html Pro modules</summary>
/// <param name="expectedText">Expected text after token replacement</param>
let verifyHtmlModuleText expectedText =    
    let htmlDivs = elements "//div[contains(@id,'HtmlModule_lblContent')]"
    let readText = (htmlDivs.Item(htmlDivs.Length-1)).Text
    if not(readText.Contains(expectedText)) then
        failwithf "  FAIL: HTML Module expected text %A is not present in actual text %A" expectedText readText

/// <summary>Chooses an available pane to edit text</summary>
let openQuickAddText()=
    let thePane = 
        if exists htmlInlineEditMask then htmlInlineEditMask
        elif exists htmlLeftPaneAddText then htmlLeftPaneAddText
        elif exists htmlTopHeroPaneAddText then htmlTopHeroPaneAddText
        elif exists htmlTopPaneAddText then htmlTopPaneAddText
        elif exists htmlTopHeroDarkPaneAddText then htmlTopHeroDarkPaneAddText
        else ""
    if String.IsNullOrEmpty thePane then failwith "  FAIL: Could not find a quick add text handler on page"
    scrollTo thePane
    click thePane
    waitForElementPresent "a.re-bold" //bold icon in toolbar
    waitForAjax()

/// <summary> Verifies if the source of specified image on the page is Users folder. Image should already be uploaded and page published. </summary>
/// <param name="imagename"> The name of the file whose source is to be verified, without extenstion. </param>
/// <returns> True if source if image is the Users directory, False otherwise. </returns>
let verifyImageUploadedToUserDirectory imagename = 
    let imageSelector = sprintf "//img[contains(@src,'%s')]" imagename
    let mutable verified = false
    if existsAndVisible imageSelector then
        let uploadedImage = element imageSelector
        if uploadedImage.GetAttribute("src").Contains("/Users/") then verified <- true
    verified

/// <summary>Enters a title and body text into HTML Module</summary>
/// <param name="moduleNumber">The module number of the HTML or HTML Pro module</param>
/// <param name="title">The title of the html module text</param>
/// <param name="body">The body text to be inserted</param>
/// <param name="containsTokens">True if body contains replacement tokens, False otherwise</param>
let private insertTextHTMLModule moduleNumber title body containsTokens =
    let htmlToInsert = sprintf "<h2>%s</h2>%s" title body
    openEditMode()
    let mutable moduleSpace = ""
    if isNull moduleNumber then        
        moduleSpace <- "//div[contains(@id,'ModuleContent')]"
    else
        moduleSpace <-  (sprintf "//div[contains(@id,'dnn_ctr%s_ModuleContent')]" (moduleNumber.ToString()))
    let openModuleToEdit()=
        try
            scrollTo moduleSpace
            sleep 0.5 //for the page to settle down
            hoverOver moduleSpace
            let pencil = "li.actionMenuEdit"
            waitForElementPresent pencil
            hoverOver pencil
            waitForElementPresent "//span[.='Edit Content']"
            clickDnnPopupLink "//span[.='Edit Content']"
            true
        with _ ->
            reloadPage()
            openEditMode()
            waitForElementPresent moduleSpace
            false
    let openSucess = retryWithWait 3 0.5 openModuleToEdit
    if not openSucess then
        failwith "  FAIL: HTML module could not be opened for editing."
    if existsAndVisible "//span[.='Source']" then
        click "//span[.='Source']"       
        waitForElementPresent "textarea.cke_source" 
        "textarea.cke_source" << htmlToInsert
    else
        click "//div[@class='reEditorModes']/ul/li/a/span[.='HTML']"
        waitForElementPresent "//td[@class='reContentCell']/iframe[2]"
        "//td[@class='reContentCell']/iframe[2]" << htmlToInsert
    click "//a[.='Save']"
    waitForSpinnerDone()
    waitPageLoad()
    closeEditMode()
    waitForElementPresent "//div[contains(@id,'HtmlModule_lblContent')]"
    if not(containsTokens) then
        let moduleText = (element "//div[contains(@id,'HtmlModule_lblContent')]").Text
        if not(moduleText.Contains(body)) then 
            failwith "  FAIL: Text could not be published in HTML Module."

/// <summary>Enters a title and body text into HTML or HTML Pro Module</summary>
/// <param name="moduleNumber">The module number of the HTML or HTML Pro module</param>
/// <param name="title">The title of the html pro module text</param>
/// <param name="body">The body text to be inserted</param>
/// <param name="containsTokens">True if body contains replacement tokens, False otherwise</param>
/// <param name="workflow">The Workflow for the page</param>
/// <param name="workflowMessage">The message to be included as part of the Workflow</param>
let insertTextHTML moduleNumber title body containsTokens =
    closePersonaBarIfOpen()
    insertTextHTMLModule moduleNumber title body containsTokens

/// <summary>Enables or disables token replacement setting of the Html module</summary>
/// <param name="enable">True to enable, False to disable</param>
/// <param name="handleNumber">Position (starting at 0, from the top) of the given module's handle on the page.</param>
let enableTokenReplacement enable handleNumber =
    let modNumber = openModuleSettings null handleNumber
    click (sprintf "#dnn_ctr%s_ModuleSettings_hlSpecificSettings" modNumber)
    let cbSpan = "//span[.='Replace Tokens:']/../../../span[contains(@class,'dnnCheckbox-checked')]"
    let isEnabled = existsAndVisible cbSpan
    if enable <> isEnabled then
        click "//span[.='Replace Tokens:']/../../../span/span/img"
    click "//a[.='Update']"
    waitForSpinnerDone()
    waitPageLoad()
