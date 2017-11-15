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

module PBPages

open System.Drawing
open canopy
open DnnCanopyContext
open DnnAddToRole
open DnnManager
open DnnUserLogin
open DnnPages

let mutable private pageName = ""
let private platformPages = ["Home"; "Activity Feed"; "Search Results"; "404 Error Page"]

/// <summary>Find the Root page on the page tree.</summary>
/// <param name="pagename">Page Name.</param>
/// <returns>Return page element true if found. false return if the page element not exist.</returns>
let private rootPageExistsSearching pagename =
    let mutable failedReasons = ""
    let thePage = sprintf "//p[.='%s']" pagename
    if existsAndVisible thePage then ""
    else
        failedReasons <- sprintf " %A. " pagename
        failedReasons

/// <summary>Verify the page exist.</summary>
/// <param name="pagename">Page Name.</param>
/// <returns>Return page element true if found. false return if the page element not exist.</returns>
let private pageDetails pagename =
     click ("//p[.='"+pagename+"']")
     let pageData = (element "//label[.='Name*']/../../div[2]/input").GetAttribute("value")
     pageData = pagename

/// <summary>Edit page and verify if page has been updated.</summary>
/// <param name="pagename">Page Name.</param>
/// <returns>Exception will happen if the page element not updated.</returns>
let private editPage pagename =
    let vPage = pageDetails pagename
    let pageDescriptionSelector = sprintf "//label[.='%s']/../../div[2]/textarea" pbDescriptionLabelText
    let pageDescription = element pageDescriptionSelector
    scrollToPoint (Point(pageDescription.Location.X,pageDescription.Location.Y-1000))
    let pageNameElement = element "div.left-column>div>div.input-tooltip-container>input"
    pageName <- "EditPage"+getRandomId()
    pageNameElement << pageName
    "div.dnn-grid-system>div:nth-child(2)>div>div>div>input" << pageName
    pageDescription << pageName
    //display in menu
    let addBtnSelector = sprintf "//div[@class='buttons-box']/button[.='%s']" editPageText
    scrollTo addBtnSelector
    click addBtnSelector
    reloadPage()
    waitPageLoad()
    if not(pageDetails pageName) then
           failwith "  FAIL: Unable to edit the page"    

/// <summary>Page Details Tab Verificatio Function.</summary>
/// <param name="pagename">Page Name.</param>
/// <returns>Return page element true if found. false return if the tab element not exist.</returns>
let private pageDetailsTabVerification pagename =
     (pageDetails pagename)
        && existsAndVisible "//li[@role='tab'][1]"
        && existsAndVisible "//li[@role='tab'][2]"
        && existsAndVisible "//li[@role='tab'][3]"
        && existsAndVisible "//li[@role='tab'][4]"

let private checkDefaultPages _ =
    let mutable failedReasons = ""
    failedReasons <- platformPages |> List.fold (fun acc  item -> acc + (rootPageExistsSearching item)) failedReasons
    if failedReasons <> "" then 
        failwithf "  FAIL: Following Page(s) does not exist %A" failedReasons 

let private adminTests _ =
    context "Persona Bar: Pages: Admin User Tests"

    "PB Pages | Admin | Check Empty State" @@@ fun _ ->
        logOff()
        loginAsAdmin() |> ignore
        openPBPages()
        if not(existsAndVisible "//div[@class='empty-page-state-message']") then
            failwith "  FAIL: A Persona Bar > Pages Empty State Case Not Available "

    "PB Pages | Admin | Verify Default Pages" @@@ fun _ ->
        checkDefaultPages()

    "PB Pages | Admin | Create a page" @@@ fun _ ->
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        let random = getRandomId()
        pageName <- "TestPage" + random
        let pageInfo = getPageInfo pageName ""
        createPagePB pageInfo null |> ignore
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false
        openPBPages()
        if not(testLevel0PageExistsPBBySearching pageName) then
            failwith "  FAIL: A page could not be created successfully in Persona Bar > Pages"

    "PB Pages | Admin | Verify Page Details Tabs" @@@ fun _ ->
        if not(pageDetailsTabVerification pageName) then
            failwith "  FAIL: A page could not be Verified in the details section"

    "PB Pages | Admin | Edit Page Test" @@@ fun _ ->
        if (rootPageExistsSearching pageName) <> "" then
            failwith "  FAIL: A page could not be Verified in the details section"
        else
            editPage pageName

    "PB Pages | Admin | Delete Page" @@@ fun _ ->
        if not(deletePageFromTree pageName) && not(isShown pageName) && not(isExist pageName)  then
            failwithf "  FAIL: Unable to delete the page %A" pageName

let all _ = 
    adminTests()
