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

module DnnPrompt

open System
open canopy

/// <summary>Check if error is displayed on the Prompt console</summary>
let private checkIfErrorDisplayed()=
    if existsAndVisible promptErrorMsg then
        let command = (element "span.dnn-prompt-cmd").Text
        let errorMsg = (element promptErrorMsg).Text
        failwithf "  FAIL: Command %A resulted in error message displayed: %s" command errorMsg

/// <summary>Verifies command output contains at least one data row</summary>
let private verifyDataDisplayed()=
    let dataRow = promptOutputTable + ">tr"
    if not(existsAndVisible dataRow) then
        failwith "  FAIL: Prompt command output did not display any data"

/// <summary>Checks that parameter is not empty or null</summary>
/// <param name="p">Parameter to be checked</param>
let private chkExists (p:string) =
    if String.IsNullOrEmpty(p.Trim()) then
        failwith "  FAIL: Parameters for this command cannot be null or empty"

let sendKeyToPrompt key =
    promptInput << key
    waitForAjax()

let sendCommandToPrompt command =
    printfn "  Info: Running Prompt Command: %s" command
    promptInput << command + enter
    waitForAjax()

/// <summary>Clears the Prompt Screen</summary>
let private clearPromptScreen()=
    sendCommandToPrompt "cls"
    checkIfErrorDisplayed()

/// <summary>Navigate to last page of command output</summary>
let private navToLastPage()=
    if existsAndVisible promptScreen then
        let mutable retries = 30
        while not(existsAndVisible promptInput) && retries > 0 do
            press enter
            sleep 0.2
            waitLoadingBar()
            retries <- retries-1

/// <summary>Runs a Prompt Command</summary>
/// <param name="command">The command to run</param>
/// <param name="waitElm">Selector of the element to wait for after running command</param>
let private runCommand command waitElm =
    if not(existsAndVisible promptInput) then openPBPrompt()
    clearPromptScreen()
    sendCommandToPrompt command
    waitLoadingBar()
    checkIfErrorDisplayed()
    if not(String.IsNullOrEmpty(waitElm)) then
        waitForElementPresent waitElm
        checkIfErrorDisplayed()
    //If output contains more pages, get to the last page
    if existsAndVisible promptScreen then navToLastPage()
    checkIfErrorDisplayed()

/// <summary>Checks if Prompt history is cleared</summary>
let private checkHistoryCleared()=
    //run previous command 'cls' to check history
    sendKeyToPrompt up
    sendKeyToPrompt enter
    checkIfErrorDisplayed()
    exists promptOutputs

/// <summary>Runs a Prompt Command</summary>
/// <param name="command">Command type to run</param>
/// <param name="parameters">Parameters of the command</param>
let runPromptCommand (command:PromptCommand) parameters =
    match command with
    | ADDMODULE ->
        chkExists parameters
        runCommand ("add-module "+parameters) null
    | ECHO ->
        chkExists parameters
        runCommand ("echo "+parameters) promptOkMsg
        let expText = 
            if parameters.Contains(" ") then
                let len = parameters.IndexOf(" ")
                parameters.Substring(0,len)
            else parameters
        let outputText = (element promptOkMsg).Text
        if outputText <> expText then
            failwithf "  FAIL: Prompt Command 'echo' output message: Expected %A, Actual %A" expText outputText
    | EXIT ->
        runCommand ("exit "+parameters) null
        if existsAndVisible promptInput then
            failwith "  FAIL: Prompt Command 'exit' did not exit prompt interface" 
    | GETSITE ->
        runCommand ("get-site "+parameters) null
    | HELP ->
        runCommand ("help "+parameters) null
    | LISTCOMMANDS ->
        runCommand ("list-commands "+parameters) null
    | LISTPORTALS ->
        runCommand ("list-portals "+parameters) null
    | LISTSITES ->
        runCommand ("list-sites "+parameters) null
    | PURGEMODULE ->
        chkExists parameters
        runCommand ("purge-module "+parameters) null
    | CLH ->        
        runCommand "clh" promptOkMsg
        if not(checkHistoryCleared()) then
            failwith "  FAIL: Prompt Command 'clh' did not clear history"        
    | CLS ->
        runCommand "cls" null
        if exists promptOutputs then
            failwith "  FAIL: Prompt Command 'cls' did not clear the screen"
    | CLEARHISTORY ->
        runCommand "clear-history" promptOkMsg
        if not(checkHistoryCleared()) then
            failwith "  FAIL: Prompt Command 'clear-history' did not clear history"  
    | CLEARSCREEN ->
        runCommand "clear-screen" null
        if exists promptOutputs then
            failwith "  FAIL: Prompt Command 'clear-screen' did not clear the screen"
    | CONFIG ->
        chkExists parameters
        runCommand ("config "+parameters) null
        let promptStyle = (element "div.dnn-prompt").GetAttribute("style")
        if not(promptStyle.Contains(parameters)) then
            failwithf "  FAIL: Prompt command 'config' could not set height to %A" parameters
    | GOTO ->
        chkExists parameters
        runCommand ("goto "+parameters) null
        waitForElementNotPresent promptInput
        waitForElementPresent promptInput
        if not(existsAndVisible promptOutputCmd) then
            failwith "  FAIL: Prompt command 'goto' did not work"
    | RELOAD ->
        runCommand "reload" null
        waitForElementNotPresent promptInput
        waitForElementPresent promptInput
        if not(existsAndVisible promptOutputCmd) then
            failwith "  FAIL: Prompt command 'reload' did not work"
    | SETMODE ->
        chkExists parameters
        runCommand ("set-mode "+parameters) null
        waitPageLoad()
    | GETPAGE ->
        chkExists parameters
        runCommand ("get-page "+parameters) promptOutputTable
        verifyDataDisplayed()
    | LISTPAGES ->
        runCommand ("list-pages "+parameters) promptOutputTable
        verifyDataDisplayed()
    | NEWPAGE ->
        chkExists parameters
        runCommand ("new-page "+parameters) promptOutputTable
        verifyDataDisplayed()
    | SETPAGE ->
        chkExists parameters
        runCommand ("set-page "+parameters) promptOutputTable
        verifyDataDisplayed()
    | DELETEPAGE ->
        chkExists parameters
        runCommand ("delete-page "+parameters) promptOkMsg
    | RESTOREPAGE ->
        chkExists parameters
        runCommand ("restore-page "+parameters) promptOkMsg
    | PURGEPAGE ->
        chkExists parameters
        runCommand ("purge-page "+parameters) promptOkMsg
    | GETUSER ->
        chkExists parameters
        runCommand ("get-user "+parameters) promptOutputTable
        verifyDataDisplayed()
    | LISTUSERS ->
        runCommand ("list-users "+parameters) promptOutputTable
        verifyDataDisplayed()
    | NEWUSER ->
        chkExists parameters
        runCommand ("new-user "+parameters) promptOutputTable
        verifyDataDisplayed()
    | SETUSER ->
        chkExists parameters
        runCommand ("set-user "+parameters) promptOutputTable
        verifyDataDisplayed()
    | DELETEUSER ->
        chkExists parameters
        runCommand ("delete-user "+parameters) promptOutputTable
        verifyDataDisplayed()
    | PURGEUSER ->
        chkExists parameters
        runCommand ("purge-user "+parameters) null
        //verifyDataDisplayed()
    | RESTOREUSER ->
        chkExists parameters
        runCommand ("restore-user "+parameters)  null
        //verifyDataDisplayed()
    | ADDROLES ->
        chkExists parameters
        runCommand ("add-roles "+parameters) promptOutputTable
        verifyDataDisplayed()
    | RESETPASSWORD ->
        chkExists parameters
        runCommand ("reset-password "+parameters) promptOkMsg
    | COPYMODULE ->
        chkExists parameters
        runCommand ("copy-module "+parameters) null
    | DELETEMODULE ->
        chkExists parameters
        runCommand ("delete-module "+parameters) null
    | LISTMODULES ->
        runCommand ("list-modules "+parameters) null
    | MOVEMODULE ->
        chkExists parameters
        runCommand ("move-module "+parameters) null
    | GETMODULE ->
        chkExists parameters
        runCommand ("get-module "+parameters) null
    | RESTOREMODULE ->
        chkExists parameters
        runCommand ("restore-module "+parameters) null
    | CLEARCACHE ->
        runCommand ("clear-cache "+parameters) null
    | GETHOST ->
        runCommand ("get-host") null
    | RESTARTAPPLICATION ->
        runCommand ("restart-application "+parameters) null
    | CLEARLOG ->
        runCommand ("clear-log "+parameters) null
    | GETPORTAL ->
        runCommand ("get-portal "+parameters) null
    | GETROLE ->
        chkExists parameters
        runCommand ("get-role "+parameters) null
    | DELETEROLE ->
        chkExists parameters
        runCommand ("delete-role "+parameters) null
    | LISTROLES ->
        runCommand ("list-roles "+parameters) promptOutputTable
        verifyDataDisplayed()
    | NEWROLE ->
        chkExists parameters
        runCommand ("new-role "+parameters) null
    | SETROLE ->
        chkExists parameters
        runCommand ("set-role "+parameters) null
    | GETTASK ->
        chkExists parameters
        runCommand ("get-task "+parameters) null
    | LISTTASKS ->
        runCommand ("list-tasks "+parameters) null
    | SETTASK ->
        chkExists parameters
        runCommand ("set-task "+parameters) null       

/// <summary>Retrieves data from the Output of a command</summary>
/// <param name="rowNum">Row number to read in the output table, -1 to read last row</param>
/// <param name="colNum">Column number to read in the output table</param>
let readOutputTableData rowNum colNum =
    let cell = 
        match rowNum with
        | -1 -> sprintf "div:last-of-type>table.dnn-prompt-tbl>tbody>tr:last-of-type>td:nth-of-type(%i)" colNum
        | _ -> sprintf "div:last-of-type>table.dnn-prompt-tbl>tbody>tr:nth-of-type(%i)>td:nth-of-type(%i)" rowNum colNum
    let dataCell = if existsAndVisible (cell+">*") then (cell+">*") else cell
    (element dataCell).Text

/// <summary>Verifies whether a page exists or is deleted</summary>
/// <param name="pageName">Name of the page</param>
/// <returns>True if page exists, false otherwise</returns>
let verifyPageExists pageName =
    closePersonaBar()
    goto pageName
    let exists = check404SitePageDisplayed()
    goto "/"
    openPBPrompt()
    not exists

/// <summary>Checks if a page is in Recycle Bin</summary>
/// <param name="pageName">Name of the page</param>
/// <returns>True if page is in recycle bin, false otherwise</returns>  
let checkPageIsInRecycleBin pageName =
    openPBRecycleBin()
    let inBin = DnnRecycleBin.checkPageInRecycleBin pageName
    openPBPrompt()
    inBin

/// <summary>Runs a Prompt Command and returns the error message</summary>
/// <param name="command">The command to run</param>
/// <returns>Error message text</returns>
let readCommandError command =
    clearPromptScreen()
    sendCommandToPrompt command
    if not(existsAndVisible promptErrorMsg) then sleep 1
    if existsAndVisible promptErrorMsg then
        (element promptErrorMsg).Text
    else ""
