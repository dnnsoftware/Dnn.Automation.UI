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

module DnnSiteCreate

open System
open canopy
open DnnUserLogin

/// <summary>Creates a child site</summary>
/// <param name="siteAlias">Alias of the child site</param>
/// <param name="siteTitle">Title of the child site</param>
/// <param name="siteGroup">Name of the site group to add site to; null for no group</param>
let createChildSite siteAlias siteTitle (siteGroup:string) =
    openPBSites()    
    let titleTB = "#add-new-site-title"
    let directoryInput = sprintf "//label[.=\"%s\"]" directoryText
    let createSiteBtn = "div.site-action-buttons>button:nth-child(2)"
    click addSiteBtn    
    waitForElementPresent titleTB
    titleTB << siteAlias    
    scrollTo createSiteBtn
    click directoryInput
    //Site URL is populated automaticaly from site alias
    sleep 0.5
    //Enter correct title
    scrollTo titleTB
    titleTB << siteTitle
    scrollTo createSiteBtn
    //add to site group
    if not(String.IsNullOrEmpty siteGroup) then
        let siteGroupDdn = "//div[@class='AddToSiteGroup']/div/div"
        let siteGroupItem = siteGroupDdn + sprintf "/div[contains(@class,'collapsible-content')]/div/div/div/div/ul/li[.=\"%s\"]" siteGroup
        if exists siteGroupItem then
            click (siteGroupDdn + "/div[@class='dropdown-icon']/*")
            waitClick siteGroupItem
            waitForAjax()
        else
            failwithf "  FAIL: Site Group %A does not exist" siteGroup
    //create site    
    click createSiteBtn
    //wait for site creation
    waitForElementPresentXSecs addSiteBtn childSiteCreationTimeout //wait for child site creation

/// <summary>Creates a child site to run Automation tests on, using configured child site alias and title</summary>
let createAutomationChildSite() = 
    useChildPortal <- false
    loginAsHost()
    createChildSite childSiteAlias childWebSiteName null
    closePersonaBar()
    //goto child site (set and reset timeouts)
    let oldTimeout = canopy.configuration.pageTimeout
    try
        canopy.configuration.pageTimeout <- childSiteCreationTimeout
        browser.Manage().Timeouts().PageLoad <- TimeSpan.FromSeconds childSiteCreationTimeout
        goto childSiteAlias
    finally
        canopy.configuration.pageTimeout <- oldTimeout
        browser.Manage().Timeouts().PageLoad <- TimeSpan.FromSeconds oldTimeout
