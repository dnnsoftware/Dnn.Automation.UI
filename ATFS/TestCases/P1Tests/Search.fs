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

module Search

open canopy
open DnnCanopyContext
open DnnUserLogin
open DnnAddUser
open DnnAddToRole
open DnnHost
open DnnAdmin
open DnnAddModuleToPage
open DnnUserProfile
open DnnScheduler
open DnnPages

let private searchUserInfo = 
    {   UserName = "TestSearchUserName" + getRandomId()
        Password = defaultPassword
        ConfirmPass = defaultPassword
        DisplayName = "TestSearchDisplayName" + getRandomId()
        EmailAddress = "TestSearchEmailAddress" + getRandomId() + "@randomdomain" + getRandomId() + ".com" }
let private searchUserFirstName = "TestFirstName" + getRandomId()
let private searchUserLastName = "TestLastName" + getRandomId()

/// <summary>Add a post in Journal module</summary>
/// <param name="posttext">The text of the post to be added</param>
let private addJournalPost posttext =
    click "#journalPlaceholder"
    sleep 0.5
    (element "#journalContent") << posttext
    sleep 0.5
    click "//a[.='Share']"
    waitPageLoad()

/// <summary>Run Host Scheduler - Search: Site Crawler</summary>
/// <returns>True if Scheduler ran successfully, False otherwise</returns>
let private runSearchSiteScheduler()=
    loginAsHost()
    let schedulerName = "Search: Site Crawler"
    canopy.configuration.skipRemainingTestsInContextOnFailure <- true        
    let runSearchScheduler = runHostSchedulerWaitComplete schedulerName 30
    canopy.configuration.skipRemainingTestsInContextOnFailure <- false
    if not runSearchScheduler then
        failwithf "  FAIL: Scheduler %A did not run successfully" schedulerName
    else
        clearAppCacheAndRecycleApp()
    runSearchScheduler

/// <summary>Testcase helper for performing a basic and advanced search for a phrase that exists</summary>
/// <param name="searchphrase">The phrase to be searched for</param>
let private testSearch searchphrase =    
    closePersonaBarIfOpen()
    let basicResults, basicCount = getSearchResultsBasic searchphrase
    if basicCount < 1 then failwithf "  FAIL: No search results found for %A" searchphrase
    else printfn "  Found %i basic search results for %A" basicCount searchphrase
    let advancedResults, advancedCount = getSearchResultsAdvanced searchphrase
    if advancedCount < 1 then failwithf "  FAIL: No search results found for %A" searchphrase
    else printfn "  Found %i advanced search results for %A" advancedCount searchphrase

/// <summary>Testcase helper for performing a basic and advanced search for a phrase that does not exist</summary>
/// <param name="searchphrase">The phrase to be searched for</param>
let private testSearchNotFound searchphrase =    
    let basicResults, basicCount = getSearchResultsBasic searchphrase
    if basicCount > 0 then failwithf "  FAIL: Found %i basic search results for %A" basicCount searchphrase
    let advancedResults, advancedCount = getSearchResultsAdvanced searchphrase
    if advancedCount > 0 then failwithf "  FAIL: Found %i advanced search results for %A" advancedCount searchphrase

/// <summary>Testcase helper to search for various attributes of a site user, by a priviliged user</summary>
let private testSearchSiteUserPriviliged() =
    testSearch searchUserInfo.UserName
    testSearch searchUserInfo.DisplayName
    testSearch searchUserInfo.EmailAddress
    testSearch searchUserFirstName
    testSearch searchUserLastName

/// <summary>Testcase helper to search for various attributes of a site user, by a normal user</summary>
let private testSearchSiteUserRegular() =
    testSearchNotFound searchUserInfo.UserName
    testSearch searchUserInfo.DisplayName
    testSearchNotFound searchUserInfo.EmailAddress
    testSearch searchUserFirstName
    testSearch searchUserLastName

let private journalPostEntry = "JournalPostEntry" + getRandomId()

let private platformUsersSearch _ =
    context "Search Tests: Platform Users: Search Existing Page"

    "Search | Admin | Search for an Existing Page" @@@ fun _ ->
        if runSearchSiteScheduler() then
            loginAsAdmin() |> ignore
            testSearch "Home"

    "Search | Host | Search for an Existing Page" @@@ fun _ ->
        loginAsHost()
        testSearch "Home"

    "Search | Regular User | Search for an Existing Page" @@@ fun _ ->
        loginAsRegularUser() |> ignore
        testSearch "Home"

    "Search | Anonymous User | Search for an Existing Page" @@@ fun _ ->
        logOff()
        testSearch "Home"

    context "Search Tests: Platform Users: Search Journal Entry"

    "Search | Admin | Search for a Journal Entry" @@@ fun _ ->
        deployModuleOnPage false "Journal" "Journal" null |> ignore
        addJournalPost journalPostEntry
        //grant view permission to all users (for search)
        grantPageAllViewPerm()
        if runSearchSiteScheduler() then
            loginAsAdmin() |> ignore
            testSearch journalPostEntry

    "Search | Host | Search for a Journal Entry" @@@ fun _ ->
        loginAsHost()
        testSearch journalPostEntry

    "Search | Regular User | Search for a Journal Entry" @@@ fun _ ->
        loginAsRegularUser() |> ignore
        testSearch journalPostEntry

    "Search | Anonymous User | Search for a Journal Entry" @@@ fun _ ->
        logOff()
        testSearch journalPostEntry

    context "Search Tests: Platform Users: Search Site Users"

    "Search | Admin | Search for a Site User" @@@ fun _ -> 
        changeUserRegistrationType UserRegistrationType.PUBLIC
        registerUser searchUserInfo ignore
        //update firstname and lastname
        loginAsRegisteredUser searchUserInfo.UserName defaultPassword
        gotoEditProfilePage()
        updateProfileProperties searchUserFirstName searchUserLastName "9440 202nd Street" "Langley" "604-555-5555"
        changeUserRegistrationType UserRegistrationType.VERIFIED
        if runSearchSiteScheduler() then
            loginAsAdmin() |> ignore
            testSearchSiteUserPriviliged()        

    "Search | Host | Search for a Site User" @@@ fun _ ->  
        loginAsHost()
        testSearchSiteUserPriviliged()

    "Search | Search User | Search for a Site User" @@@ fun _ ->  
        loginAsRegisteredUser searchUserInfo.UserName defaultPassword
        testSearchSiteUserPriviliged()

    "Search | Regular User | Search for a Site User" @@@ fun _ ->  
        loginAsRegularUser() |> ignore
        testSearchSiteUserRegular()

    "Search | Anonymous User | Search for a Site User" @@@ fun _ ->  
        logOff()
        testSearchSiteUserRegular()

let all _ =
    platformUsersSearch()

