﻿// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
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

module PBTools

open canopy
open DnnCanopyContext
open DnnCommon
open DnnUserLogin

let mutable private defaultCss = ""
let private restoreDefBtn = "button.cancel[data-bind*='restoreStyleSheet']"
let saveSheetBtn = "button.create-page[data-bind*='saveStyleSheet']"

/// <summary>Saves SQL Query button</summary> 
/// <param name="queryName">Name to save the sql query with</param>
let private saveSqlQuery queryName =     
    click "button[data-bind*='SaveQuery']"
    waitForAjax()
    "#query-name" << queryName
    click "//a[@class='btn save']" 
    waitForAjax()

/// <summary>Restores the default CSS of a site</summary>
let private restoreDefaultCss()=
    scrollTo restoreDefBtn
    click restoreDefBtn
    waitClick "#confirmbtn"
    //wait for notification to appear and disappear
    waitForElementPresent "div#notification-dialog[style*=block]"
    waitForElement "div#notification-dialog[style*=none]"
    let theCss = (element customCssEditor).Text
    if theCss.Length < 10 then
        failwith "  FAIL: Default CSS loaded was less than 10 characters in length"
    theCss

let private cssEditorTests _ =

    context "Custom CSS Editor Tests"

    "Custom CSS | Validating UI and default CSS" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        logOff()
        loginAsHost() 
        openPBCustomCss()
        //Verify UI
        scrollTo restoreDefBtn
        if not(existsAndVisible restoreDefBtn) || not(existsAndVisible saveSheetBtn) then
            failwith "  FAIL: Restore Default Btn or Save Style Sheet Btn is not visible"
        //load default CSS
        defaultCss <- restoreDefaultCss()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false               

    "Custom CSS | Verify CSS can be updated" @@@ fun _ ->
        let newCss = "New CSS Test " + getRandomId()
        scrollToOrigin()
        let codeMirrorElement = element customCssEditor
        click codeMirrorElement
        execJs ("arguments[0].CodeMirror.setValue(\"" + newCss + "\");") codeMirrorElement |> ignore
        scrollTo saveSheetBtn
        click saveSheetBtn
        waitForElementPresent "div#notification-dialog[style*=block]"
        reloadPage()
        openPBCustomCss()
        let currentCss = (element customCssEditor).Text
        if not(currentCss.Contains(newCss)) then
            failwith "  FAIL: New CSS could not be saved."

    "Custom CSS | Verify default CSS can be restored" @@@ fun _ ->        
        let currentCss = restoreDefaultCss()
        if currentCss <> defaultCss then
            failwith "  FAIL: Default CSS could not be restored."

let private sqlConsoleTests _ =

    context "SQL Console Tests"

    "Sql Console | Querying in the Sql Editor" @@@ fun _ ->
        logOff()
        loginAsHost() 
        openPBSqlConsole()
        let sqlQuery = "SELECT * FROM {databaseOwner}{objectQualifier}Tabs"
        executeSqlQuery sqlQuery
        let resultHeader = (element "//div[@class='jspPane']/table/thead/tr/th[1]").Text
        if not(resultHeader.Contains("TabID")) then
            failwithf "  FAIL: Results Table was not displayed after executing Sql Query %A" sqlQuery

    "Sql Console | Save and Delete Query " @@@ fun _ ->
        let savedQueryName =  "Sql Query " + getRandomId()
        saveSqlQuery savedQueryName
        let trashIcon = "//a[@class='btn-trash']"
        waitForElementPresent trashIcon
        //delete query
        click trashIcon
        waitClick "#confirmbtn"
        waitForAjax()
        if existsAndVisible trashIcon then
            failwith "  FAIL: Saved Sql Query could not be deleted"

let all _ =
    cssEditorTests()
    sqlConsoleTests()        
