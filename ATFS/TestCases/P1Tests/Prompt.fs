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

module Prompt

open canopy
open DnnCanopyContext
open DnnUserLogin
open DnnPrompt

let mutable private pageId = ""
let mutable private outputMessage = ""
let mutable private outputCommand = ""
let mutable private pageName = ""
let mutable private userName = ""
let mutable private userId = ""
let mutable private taskId = ""
let mutable private roleId = ""
let mutable private roleName =""
let mutable private moduleId = ""
let mutable private pgIdForModuleId = ""

/// <summary>Ckeck the Page in Persona bar </summary>
/// <param name="pageName">Name of Page </param>
/// <param name="pageTitle">Title of Page </param>
/// <param name="pageAction">Action perform on the Page </param>
let private checkPBPage  pageName pageTitle pageAction =
    if pageAction = "newPage" then
        openPBPages()
        if not( DnnPages.isExist pageName )  then
            failwithf "  FAIL: pageName : %s is not Exist" pageTitle  
    if pageAction = "editPage" then
        openPBPages()
        let pageName1 = sprintf "//p[.='%s']" pageName
        click pageName1
        let titleValue = sprintf "//label[text()='Title']/../../div[2]/input[@value='%s']" pageTitle
        waitForElementPresent titleValue
        if  not (existsAndVisible titleValue)  then
            failwithf "  FAIL: Page value: %s is not saved" pageTitle
    if pageAction = "deletePage" then
        openPBRecycleBin()
        let titleValue = sprintf"//div[@class='pagename' and text() = '%s']" pageName
        if not (existsAndVisible titleValue)  then
            failwithf "  FAIL: Page value: %s is not saved" pageTitle
    if pageAction = "restorePage" then
        openPBPages()
        let pageTitle1 = sprintf "//p[.='%s']" pageName
        if not( existsAndVisible pageTitle1 )  then
            failwithf "  FAIL: pageName : %s is not Exist" pageName      
    if pageAction = "purgePage" then 
        openPBRecycleBin()
        let titleValue = sprintf"//div[@class='pagename' and text() = '%s']" pageName
        if existsAndVisible titleValue  then
            failwithf "  FAIL: Page value: %s is not purge" pageName                

/// <summary>Ckeck the User in Persona bar </summary>
/// <param name="UserName">Name of Page </param>
/// <param name="displayName">Display name of User </param>
/// <param name="userAction">Action perform on the User </param>
let private checkPBUser  userName displayName userAction =
    if userAction = "newUser" then
        openPBUsers()
        let userName = sprintf "//p[.='%s']" userName
        if not( existsAndVisible userName )  then
            failwithf "  FAIL: userName : %s is not Exist" userName   
    if userAction = "editUser" then
        openPBUsers()
        let editName = sprintf "//p[text()='%s']/../../div[4]/div[2]/*" userName
        click editName
        let displayName = sprintf"//Input[@value='%s']"displayName
        waitForElementPresent displayName
        if  not (existsAndVisible displayName)  then
            failwithf "  FAIL: User display: %s is not saved" displayName
    if userAction = "deleteUser" then
        openPBRecycleBin()
        waitForElement "//a[@href='#users']"
        click"//a[@href='#users']"
        let userValue = sprintf"//td[text() = '%s']" userName
        if not (existsAndVisible userValue)  then
            failwithf "  FAIL: User value: %s is not deleted" userName
    if userAction = "restoreUser" then
        openPBUsers()
        let userName = sprintf "//p[.='%s']" userName
        if not( existsAndVisible userName )  then
            failwithf "  FAIL: userName : %s is not restore" userName     
    if userAction = "purgeUser" then 
        openPBRecycleBin()
        waitForElement "//a[@href='#users']"
        click"//a[@href='#users']"
        let userValue = sprintf"//td[text() = '%s']" userName
        if existsAndVisible userValue  then
            failwithf "  FAIL: User value: %s is not purge" userName
    if userAction = "addRole" then 
        openPBUsers()
        let editName = sprintf "//p[text()='%s']/../../div[4]/div[4]/*" userName
        click editName
        let roleValue = sprintf"//div[text() = '%s']" displayName
        if existsAndVisible roleValue  then
            failwithf "  FAIL: User role: %s is not purge" roleValue

/// <summary>Ckeck the Role in Persona bar </summary>
/// <param name="roleName">Name of role </param>
/// <param name="userAction">Action perform on the Role </param>
let private checkPBRole roleName userAction =
    if userAction = "newRole" then
        openPBRoles()
        let roleName = sprintf"//div[.='%s']" roleName   
        if not( existsAndVisible roleName )  then
            failwithf "  FAIL: roleName %s is not Exist" roleName  
    if userAction = "editRole" then
        openPBRoles()
        let autoAssign = sprintf "//div[text()='%s']/../../div/div[4]/span/*" roleName
        if  not (existsAndVisible autoAssign)  then
            failwithf "  FAIL: Role: %s is not assign " roleName
    if userAction = "deleteRole" then
        openPBRoles()
        let roleName = sprintf"//div[.='%s']" roleName   
        if existsAndVisible roleName  then
            failwithf "  FAIL: roleName  %s is not Deleted" roleName         

/// <summary>Ckeck the Command output </summary>
/// <param name="command">type of Command </param>
let private readOutputTexts() =
    if existsAndVisible promptErrorMsg then
        outputMessage <- (element promptErrorMsg).Text 
    if existsAndVisible promptOkMsgLast then
        outputMessage <- (element promptOkMsgLast).Text 

/// <summary>Run the Prompt Command </summary>
/// <param name="command">type of Command </param>
/// <param name="Parameter">type of Command </param>
let private runPromptCmd command parameter =
    openPBPrompt()
    sendCommandToPrompt (command + " " + parameter)

/// <summary>Ckeck the Command executed </summary>
/// <param name="command">type of Command </param>
let private singleInputcommandCheck command =
    let outputError = "//span[@class='dnn-prompt-error']"
    if existsAndVisible outputError then
        failwithf "  FAIL: Command %s is not executed successfully" command  

/// <summary>Retrieve the page id for Page created</summary>
let private getPageId pageName =
     let pageIdValue = sprintf "//td[@class='dnn-prompt-lbl' and text()='Name']/../td[3][text()='%s']/../../tr[5]/td[text()='Tab Id']/../td[3]/a" pageName
     pageId <- (element pageIdValue).Text   

/// <summary>Retrieve the User id for User created</summary>
let private getUserId() =
     let userIdValue = "//td[@class='dnn-prompt-lbl' and text()='User Id']/../td[3]/a"
     userId <- (element userIdValue).Text

/// <summary>Retrieve the Task id for User created</summary>
let private getTaskId() = 
    let taskIdValue = "//table[@class='dnn-prompt-tbl']/tbody/tr/td"
    taskId <- (element taskIdValue).Text

/// <summary>Retrieve the Role id for User created</summary>
let private getRoleId() = 
    let roleIdValue = "//td[@class='dnn-prompt-lbl' and text()='Role Id']/../td[3]/a"
    roleId <- (element roleIdValue).Text

/// <summary>Retrieve the Module id for Module </summary>
/// <param name="moduleName">Name of Module </param>
let private getModuleID moduleName = 
    let moduleIdValue = sprintf "//td[text()=' %s']/../td[1]/a" moduleName
    moduleId <- (element moduleIdValue).Text
    let pgIdofModule = sprintf "//td[text()=' %s']/../td[7]" moduleName
    pgIdForModuleId <- (element pgIdofModule).Text

/// <summary>Exit from Output paging by pressing Ctrl+x</summary>
let private exitOutputPaging() = 
    if not (existsAndVisible promptInput) then
        sendCtrlPlusKey "x"

/// <summary>Check the Admin log after Command execute</summary>
/// <param name="cmdName">Name of Command </param>
/// <param name="outputMessage">output message of Command </param>   
let private checkAdminLog cmdName outputMessage =
    reloadPage()
    openPBAdminLogs()
    //open log
    let theLog = sprintf "//div[@class='term-label-summary']/div/span[contains(.,%A)]" cmdName  
    click (first theLog)
    let logDetail = "//div[@class='logitem-collapsible' and not(contains(@style,'hidden'))]/div/div[@class='log-detail']"
    waitForElementPresent logDetail
    sleep 0.1 //wait to fully open
    let readMsg = (element logDetail).Text
    if not(readMsg.Contains(outputMessage)) then
        failwithf "  FAIL: Expected message %A not found in Admin logs.\n\tAdmin Log: %s" outputMessage readMsg

let private generalCommands _ =

    context "DnnPrompt General Commands"

    "DnnPrompt | Clh" @@@ fun _ ->
        logOff()
        loginAsHost()
        runPromptCommand CLH ""

    "DnnPrompt | Cls" @@@ fun _ ->
        runPromptCommand CLS ""

    "DnnPrompt | Clear-history" @@@ fun _ ->
        runPromptCommand CLEARHISTORY ""

    "DnnPrompt | Clear-screen" @@@ fun _ ->
        runPromptCommand CLEARSCREEN ""

    "DnnPrompt | Config" @@@ fun _ ->
        runPromptCommand CONFIG "50%"
        runPromptCommand CONFIG "100%"

    "DnnPrompt | Reload" @@@ fun _ ->
        runPromptCommand RELOAD ""

    "DnnPrompt | Echo" @@@ fun _ ->
        runPromptCommand ECHO "Hello World!"

    "DnnPrompt | Setmode" @@@ fun _ ->
        let layoutMode = "div#dnn_ContentPane.paneOutline"
        let editMode = "div#dnn_ContentPane.dnnSortable"
        //layout
        runPromptCommand SETMODE "layout"
        closePersonaBarIfOpen()        
        if not(existsAndVisible layoutMode) then
            failwith "  FAIL: Command set-mode layout: Page not in layout mode"        
        //edit
        runPromptCommand SETMODE "edit"
        closePersonaBarIfOpen()
        if not(existsAndVisible editMode) then
            failwith "  FAIL: Command set-mode edit: Page not in edit mode" 
        //view
        runPromptCommand SETMODE "view"
        closePersonaBarIfOpen()
        if existsAndVisible layoutMode || existsAndVisible editMode then
            failwith "  FAIL: Command set-mode view: Page not in view mode"        

    "DnnPrompt | Exit" @@@ fun _ ->
        runPromptCommand EXIT ""

let private pageCommands _ =

    context "DnnPrompt Page Commands"

    "DnnPrompt | New-page" @@@ fun _ ->
        logOff()
        loginAsHost()
        pageName <- "TestPage" + getRandomId()
        runPromptCommand NEWPAGE pageName
        pageId <- readOutputTableData 5 3
        readOutputTexts()
        checkAdminLog "new-page" outputMessage
        checkPBPage  pageName "" "newPage"
        //pageId is displayed on row #5 and col #3
        if not(verifyPageExists pageName) then
            failwithf "  FAIL: Command new-page: New page %A with id %A could not be created" pageName pageId

    "DnnPrompt | Get-page" @@@ fun _ ->
        runPromptCommand GETPAGE pageId
        let readPageName = readOutputTableData 6 3
        if readPageName <> pageName then
            failwithf "  FAIL: Command get-page: Read page name %A did not match expected page name %A" readPageName pageName

    "DnnPrompt | Goto" @@@ fun _ ->
        runPromptCommand GOTO pageId
        let theUrl = currentUrl().ToString()
        if not( theUrl.Contains(pageName) ) then
            failwithf "  FAIL: Command Goto: Url %A did not contain expected page name %A" theUrl pageName            

    "DnnPrompt | List-pages" @@@ fun _ ->
        runPromptCommand LISTPAGES ""
        exitOutputPaging()
        let readPageId = readOutputTableData -1 1
        let readPageName = readOutputTableData -1 3
        if readPageId <> pageId || readPageName <> pageName then
            failwithf "  FAIL: Command list-pages: Read page id %A and name %A did not match expected page id %A and name %A"  readPageId readPageName pageId pageName        

    "DnnPrompt | Delete-page" @@@ fun _ ->
        runPromptCommand DELETEPAGE pageId //soft delete
        waitForElementPresent promptInput
        readOutputTexts()
        if verifyPageExists pageName then
            failwithf "  FAIL: Command delete-page: Page %A with id %A could not be deleted" pageName pageId
        if not(checkPageIsInRecycleBin pageName) then
            failwithf "  FAIL: Command delete-page: Page %A with id %A not in Recycle Bin" pageName pageId

    "DnnPrompt | Restore-page" @@@ fun _ ->
        runPromptCommand RESTOREPAGE pageId
        waitForElementPresent promptInput
        readOutputTexts()
        checkPBPage  pageName "" "restorePage"
        if not(verifyPageExists pageName) then
            failwithf "  FAIL: Command restore-page: Page %A with id %A could not be restored" pageName pageId
        if checkPageIsInRecycleBin pageName then
            failwithf "  FAIL: Command restore-page: Page %A with id %A is in Recycle Bin" pageName pageId

    "DnnPrompt | Purge-page" @@@ fun _ ->
        runPromptCommand DELETEPAGE pageId //soft delete
        runPromptCommand PURGEPAGE pageId //hard delete
        readOutputTexts() 
        checkPBPage  pageName "" "purgePage"
        if verifyPageExists pageName then
            failwithf "  FAIL: Command purge-page: Page %A with id %A could not be purged" pageName pageId
        if checkPageIsInRecycleBin pageName then
            failwithf "  FAIL: Command purge-page: Page %A with id %A is in Recycle Bin" pageName pageId

let private userCommands _ =

    context "DnnPrompt User Commands"

    "DnnPrompt | List-users" @@@ fun _ ->
        logOff()
        loginAsHost()
        runPromptCommand LISTUSERS ""
        readOutputTexts()

    "DnnPrompt | new-user" @@@ fun _ ->
        userName <- "NewUser" + getRandomId()
        let userParams = sprintf "--username %s --email %s@myemail.com --firstname NewUser --lastname PromptTest" userName userName
        runPromptCommand NEWUSER userParams
        getUserId()
        readOutputTexts()
        checkAdminLog "new-user" outputMessage
        checkPBUser userName "" "newUser"

    "DnnPrompt | get-user" @@@ fun _ ->
        runPromptCommand GETUSER "1"
        outputCommand <- "host"
        let userName= (element"//table[@class='dnn-prompt-tbl']/tbody/tr[2]/td[3]/a").Text
        if userName <> outputCommand then
            failwithf "  FAIL: get-user Command is not get %s" outputCommand
        outputCommand <- ""

    "DnnPrompt | reset-password" @@@ fun _ ->
        runPromptCommand RESETPASSWORD userId
        readOutputTexts()

    "DnnPrompt | set-user" @@@ fun _ ->
        let userNamenew = "DNNUSERNEW" + getRandomId()
        let setuserCommand = sprintf "%s --firstname %A --lastname %A" userId userNamenew userNamenew
        runPromptCommand SETUSER setuserCommand
        waitForElementPresent promptInput
        readOutputTexts()
        checkPBUser userName userNamenew "editUser"

    "DnnPrompt | delete-user" @@@ fun _ ->
        runPromptCommand DELETEUSER userId
        readOutputTexts()
        checkPBUser userName "" "deleteUser"

    "DnnPrompt | restore-user" @@@ fun _ ->
        outputCommand <- sprintf "User with id %A and name %A restored successfully." userId userName
        runPromptCommand RESTOREUSER userId 
        waitForElementPresent promptInput
        readOutputTexts()
        checkPBUser userName "" "restoreUser"

    "DnnPrompt | add-roles" @@@ fun _ ->
        outputCommand <- "Administrators"
        let userParameter = sprintf "--id %s --roles %s" userId outputCommand
        runPromptCommand ADDROLES userParameter 
        waitForElementPresent promptInput
        let outputTableVal = (element "//table[@class='dnn-prompt-tbl']/tbody/tr/td[2]").Text
        if outputTableVal <> outputCommand then
            readOutputTexts()
            failwithf "  FAIL: No Confirmation Message: %s  appear" outputCommand
        readOutputTexts()
        checkPBUser userName "" "addRole"

    "DnnPrompt | purge-user" @@@ fun _ ->
        runPromptCommand DELETEUSER userId 
        runPromptCommand PURGEUSER userId
        readOutputTexts() 
        checkPBUser userName "" "purgeUser"

let private hostCommands _ =

    context "DnnPrompt Host Commands"

    "DnnPrompt | clear-cache" @@@ fun _ ->
        logOff()
        loginAsHost()
        runPromptCommand CLEARCACHE ""
        waitForElementPresent promptInput
        readOutputTexts() 

    "DnnPrompt | get-host" @@@ fun _ ->
        runPromptCommand GETHOST ""

    "DnnPrompt | restart-application" @@@ fun _ ->
        runPromptCommand RESTARTAPPLICATION ""
        waitForElementPresent promptInput
        readOutputTexts()
        outputCommand <- "Application Restarted"

let private portalCommands _ =

    context "DnnPrompt Portal Commands"

    "DnnPrompt | clear-log" @@@ fun _ ->
        logOff()
        loginAsHost()
        runPromptCommand CLEARLOG ""
        waitForElementPresent promptInput
        outputCommand <- "Event Log Cleared."

    "DnnPrompt | get-portal" @@@ fun _ ->
        runPromptCommand GETPORTAL ""

    "DnnPrompt | list-sites" @@@ fun _ ->
        runPromptCommand LISTSITES ""

    "DnnPrompt | list-portals" @@@ fun _ ->
        runPromptCommand LISTPORTALS ""

let private schedulerCommands _ =

    context "DnnPrompt Scheduler Commands"

    "DnnPrompt | list-tasks" @@@ fun _ ->
        logOff()
        loginAsHost()
        runPromptCommand LISTTASKS ""
        exitOutputPaging()
        waitForElementPresent promptInput
        getTaskId()
        if not (existsAndVisible "//table[@class='dnn-prompt-tbl']") then
            failwith "  FAIL: list task did not display the task in list"
        readOutputTexts()

    "DnnPrompt | get-task" @@@ fun _ ->
        runPromptCommand GETTASK taskId

    "DnnPrompt | set-task" @@@ fun _ ->
        let userParameter = sprintf "%s --enabled true" taskId
        runPromptCmd "set-task" userParameter
        waitForElementPresent promptInput
        readOutputTexts()
        if outputMessage <> outputCommand then
            let userParameter = sprintf "%s --enabled false" taskId
            runPromptCmd "set-task" userParameter
            waitForElementPresent promptInput
            readOutputTexts()
        checkAdminLog "set-task" outputMessage

let private rolesCommands _ =

    context "DnnPrompt Roles Commands"

    "DnnPrompt | new-role" @@@ fun _ ->
        logOff()
        loginAsHost()    
        roleName <- "General Public" + getRandomId()
        let roleDesc = "Role for all users"
        let userParameter = sprintf "%A --description %A --public true --autoassign false" roleName roleDesc
        runPromptCommand NEWROLE userParameter
        waitForElementPresent promptInput
        getRoleId()
        readOutputTexts()
        checkAdminLog "new-role" outputMessage
        checkPBRole roleName "newRole"

    "DnnPrompt | get-role" @@@ fun _ ->
        runPromptCommand GETROLE roleId
        waitForElementPresent promptInput
        singleInputcommandCheck "get-role"

    "DnnPrompt | set-role" @@@ fun _ ->
        let userParameter = sprintf "%s --autoassign true" roleId
        runPromptCommand SETROLE userParameter
        waitForElementPresent promptInput
        readOutputTexts()
        checkPBRole roleName "editRole"

    "DnnPrompt | list-roles" @@@ fun _ ->
        runPromptCommand LISTROLES ""
        exitOutputPaging()
        waitForElementPresent promptInput
        readOutputTexts()

    "DnnPrompt | delete-role" @@@ fun _ ->
        runPromptCommand DELETEROLE roleId
        waitForElementPresent promptInput
        readOutputTexts()
        checkPBRole roleName "deleteRole"

let private moduleCommands _ =

    context "DnnPrompt Module Commands"

    "DnnPrompt | list-module" @@@ fun _ ->
        logOff()
        loginAsHost()
        runPromptCommand LISTMODULES ""
        exitOutputPaging()
        let moduleName = "Home Banner"
        getModuleID moduleName
        readOutputTexts()

    "DnnPrompt | get-module" @@@ fun _ ->
        let userParameter = sprintf"--id %s --pageid %s" moduleId  pgIdForModuleId
        runPromptCommand GETMODULE userParameter

    "DnnPrompt | copy-module" @@@ fun _ ->
        // create a new page
        pageName <- "PageName" + getRandomId()
        runPromptCommand NEWPAGE pageName
        getPageId pageName // geting page Id
        runPromptCommand CLS ""      
        let userParameter = sprintf "%s --pageid %s --topageid %s" moduleId pgIdForModuleId pageId
        runPromptCommand COPYMODULE userParameter
        readOutputTexts()

    "DnnPrompt | delete-module" @@@ fun _ ->
        let userParameter = sprintf"%s --pageid %s" moduleId pageId
        runPromptCommand DELETEMODULE userParameter
        waitForElementPresent promptInput
        readOutputTexts()

    "DnnPrompt| restore-module" @@@ fun _ ->
        outputCommand <- sprintf "Module with id %A restored successfully."moduleId 
        let userParameter = sprintf"%s --pageid %s" moduleId pageId
        runPromptCommand RESTOREMODULE userParameter
        waitForElementPresent promptInput
        readOutputTexts()
        checkAdminLog "restore-module" outputMessage

    "DnnPrompt | purge-module" @@@ fun _ ->
        outputCommand <- "Module deleted successfully."
        let userParameter = sprintf"%s --pageid %s" moduleId pageId
        runPromptCommand DELETEMODULE userParameter
        let userParameter = sprintf"%s --pageid %s" moduleId pageId     
        runPromptCommand PURGEMODULE userParameter
        waitForElementPresent promptInput
        readOutputTexts()

    "DnnPrompt | move-module" @@@ fun _ ->
        runPromptCommand LISTMODULES ""
        exitOutputPaging()
        waitForElementPresent promptInput
        let moduleName = "Journal"
        getModuleID moduleName            
        let userParameter = sprintf "%s --pageid %s --topageid %s" moduleId pgIdForModuleId pageId
        runPromptCommand MOVEMODULE userParameter
        waitForElementPresent promptInput
        readOutputTexts()

let all _ = 
    generalCommands()
    pageCommands()
    userCommands()
    hostCommands()
    portalCommands()
    schedulerCommands()
    rolesCommands()
    moduleCommands()

