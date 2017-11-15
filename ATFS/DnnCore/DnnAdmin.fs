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

module DnnAdmin

open OpenQA.Selenium
open canopy
open DnnAddToRole

/// <summary>Changes the default user registration type for the website</summary>
/// <param name="newtype">Public, Private, Verified, or None</param>
let changeUserRegistrationType ( newtype : UserRegistrationType ) =
    loginAsAdmin() |> ignore
    openPBSecurity()
    click "div.securitySettings-app>div>div>div>div>ul>li:nth-child(2)" //Member Accounts tab
    waitForAjax()
    //Registration Settings Tab
    let hostRegSettingsTab = "div.securitySettings-app>div>div>div>div>div>div>ul>li:nth-child(2)>div"
    let regSettingsTab = 
        if exists hostRegSettingsTab then hostRegSettingsTab
        else "div.securitySettings-app>div>div>div>div>div>div>ul>li:nth-child(1)"
    click regSettingsTab 
    waitForAjax()    
    let radioNum =  match newtype with NONE -> "1" | PRIVATE -> "2" | PUBLIC -> "3" | VERIFIED -> "4"
    let userRegRadioBtn = sprintf "div[role=tabpanel]>div>div.dnn-ui-common-input-group:first-child>div.registrationSettings-row-options>div:nth-child(2)>ul>li:nth-child(%s)>label" radioNum
    click userRegRadioBtn
    let saveBtn = "div[role=tabpanel]>div>div.buttons-box:nth-child(27)>button[role=primary]"
    scrollTo saveBtn
    click saveBtn
    waitForAjax()

/// <summary>
/// Changes a user's password
/// </summary>
/// <param name="userName">Username of the user whose password is to be changed</param>
/// <param name="newPassword">New password for the user</param>
let changeUserPassword userName newPassword = 
    openPBUsers()
    let userNamePara = sprintf "//p[.='%s']" userName
    //If userName not visible, search for user
    if not(existsAndVisible userNamePara) then
        let searchBox = "div.users-filter-container>div>div.search-filter>div>input"
        searchBox << userName
        sleep 0.5
        waitForAjax()
        waitForElementPresent userNamePara
    let ellipsis = userNamePara + "/../../div[5]/div/div[contains(@class,'extension-action')]/*"
    click ellipsis
    let changePasswordLink = ellipsis + sprintf "/../../div[contains(@class,'dnn-user-menu')]/ul/li[.='%s']" changePasswordText
    waitForElementPresent changePasswordLink
    click changePasswordLink
    waitForAjax()
    let passwordField1 = "div.dnn-user-change-password>div>div>div:nth-of-type(2)>div.input-tooltip-container>input"
    let passwordField2 = "div.dnn-user-change-password>div>div>div:nth-of-type(3)>div.input-tooltip-container>input"
    waitForElementPresent passwordField1
    passwordField1 << newPassword
    passwordField2 << newPassword
    click "div.dnn-grid-system>div>button[role=primary]" //Apply button
    waitForAjax()

let getFolderTypeName foldertype =
    let mutable ftype = ""
    match foldertype with
    | STANDARD -> ftype <- "Standard"
    | SECURE -> ftype <- "Secure"
    | DATABASE -> ftype <- "Database"
    ftype

/// <summary>Perform Basic Search</summary>
/// <param name="searchphrase">The phrase to be searched for</param>
/// <returns>A list of search result elements (in searchResults), and a total count of searchResults found (in resultsCount). </returns>
let getSearchResultsBasic searchphrase =
    goto "/"
    click "span.search-toggle-icon"
    sleep 0.5
    waitForElementPresent "#dnn_dnnSearch_txtSearch"
    "#dnn_dnnSearch_txtSearch" << searchphrase
    sleep 0.5
    waitForAjax()
    try
        waitForElementPresentXSecs "ul.searchSkinObjectPreview" 3.0
        let searchResultsHeader = element "ul.searchSkinObjectPreview"
        let results = "//li[contains(@data-url,'http')]"
        if existsAndVisible results then
            let searchResults = searchResultsHeader |> elementsWithin results
            let resultsCount = searchResults.Length
            searchResults, resultsCount
        else
            List.empty<IWebElement>, 0
    with _ -> List.empty<IWebElement>, 0

/// <summary>Perform Advanced Search</summary>
/// <param name="searchphrase">The phrase to be searched for</param>
/// <returns>A list of search result elements (in searchResults), and a total count of searchResults found (in resultsCount). </returns>
let getSearchResultsAdvanced searchphrase =
    goto "/Search-Results"
    "#dnnSearchResult_dnnSearchBox_input" << searchphrase
    sleep 0.5
    waitForAjax()
    try
        waitForElementPresentXSecs "div.dnnSearchResultContainer" 3.0
        let searchResultsHeader = element "div.dnnSearchResultContainer"
        let results = "div.dnnSearchResultItem-Title>a"
        if existsAndVisible results then
            let searchResults = searchResultsHeader |> elementsWithin results
            let resultsCount = searchResults.Length
            searchResults, resultsCount
        else
            List.empty<IWebElement>, 0
    with _ -> List.empty<IWebElement>, 0
