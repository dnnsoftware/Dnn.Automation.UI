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

module PBConnectors

open canopy
open DnnCanopyContext
open DnnUserLogin
open DnnConnector

let mutable private  notificationMessage = ""
let mutable private expNotificationMsg = ""

/// <summary>Ckeck the Azure Connector Notification message  </summary>
/// <param name="azureAccName">Azure Account Name </param>
/// <param name="azureAccKey">Azure Account Key  </param>
/// <param return ="notificationMessage">Error Notification message return </param>
let private checkAzureConnNotifMsg azureAccName azureAccKey = 
    let azureConnect ="//span[@class='socialnetwork-name' and text()='Azure']/../../../tr[4]"
    let connect = "//span[@class='socialnetwork-name' and text()='Azure']/../../td[4]/a[2]"
    click connect
    let azureAccountNameInput = azureConnect + "/td/div/div/div/div/input[@type='text']"
    azureAccountNameInput << azureAccName
    let azureAccountKeyInput = azureConnect + "/td/div/div/div/div/input[@type='password']"
    azureAccountKeyInput << azureAccKey
    let savebtn = azureConnect + "/td/div/div[2]/div[2]/a[@class='primarybtn']"
    click savebtn
    waitForElement "//p [@id='notification-message']"
    if existsAndVisible "//select[@id='AzureFoldersDropDown']" then
        click savebtn
        waitForElement "//p [@id='notification-message']"
    notificationMessage <- (element "//p [@id='notification-message']").Text
    let cancelbtn = azureConnect + "/td/div/div[2]/div[1]/a[@class='secondarybtn']"
    click cancelbtn

let private platformTests _ =

    context "Connectors | Platform Connectors Tests"

    "Connectors | Configure Azure Connector" @@@ fun _ ->
        logOff()
        loginAsHost()
        addConnectorPB ConnectorsList.AZURE false

let private multipleConnectors _ =

    context "Connectors | Multiple Account Connectors Tests"

    "Connectors | Configure Multiple Azure Connectors" @@@ fun _ ->
        addMultiConnectorPB AZURE

    "Connectors | Configure Multiple UNC Connectors" @@@ fun _ ->
        addMultiConnectorPB UNC

let private negativeTests _ =

    context "Connectors | Entering incorrect values"

    "Connectors | Entering incorrect values should not get saved" @@@ fun _ ->
        logOff()
        loginAsHost()
        openPBConnectors()
        negConnectorPB ConnectorsList.AZURE true

let azureRegTests _ =

    context "Connectors | Azure Connectors  Negative Tests"

    "Connectors |  Azure | Check invalid Account name contain space in between throw error" @@@ fun _ ->
        logOff()
        loginAsHost() 
        openPBConnectors()
        expNotificationMsg <- "Account Key cannot be empty." 
        checkAzureConnNotifMsg "azureAccName dfdsfad" ""
        if expNotificationMsg <> notificationMessage then
            failwithf " Fail : Incorrect Notification  : %A " notificationMessage

    "Connectors |  Azure | Check invalid Account key contain space throw error" @@@ fun _ ->
        expNotificationMsg <- "Input is not a valid Account Key." 
        checkAzureConnNotifMsg "azureAccName dfdsfad" "dsafasf adsf"
        if expNotificationMsg <> notificationMessage then
            failwithf " Fail : Incorrect Notification  : %A " notificationMessage

    "Connectors |  Azure | verify that invalid Account key contain special character throw error" @@@ fun _ ->
        expNotificationMsg <- "Account Key cannot be empty." 
        checkAzureConnNotifMsg "azureAccName @@@$@$@#dfdsfad" ""
        if expNotificationMsg <> notificationMessage then
            failwithf " Fail : Incorrect Notification  : %A " notificationMessage

    "Connectors |  Azure | Check invalid Account key contain space  and special character throw error" @@@ fun _ ->
        expNotificationMsg <- "Input is not a valid Account Key." 
        checkAzureConnNotifMsg "azureAccName @@@$@$@#dfdsfad" "dsafadsfda@$@#$#@$"
        if expNotificationMsg <> notificationMessage then
            failwithf " Fail : Incorrect Notification  : %A " notificationMessage

    "Connectors |  Azure | verify that empty container throw error" @@@ fun _ ->
        expNotificationMsg <- "The Container Name introduced is not valid."
        checkAzureConnNotifMsg "dnn64krmv" "vV3o7id6uWNn9NPtGEFxGFDZLgNm0zCTXf/uSQlvPwMQjdSlciBPqGCHAv7WDER8DUx77LDxLpEzobztrDqBrQ=="
        if expNotificationMsg <> notificationMessage then
            failwithf " Fail : Incorrect Notification  : %A " notificationMessage

    "Connectors |  Azure | check Account Name and Account key empty and notification message appear" @@@ fun _ ->
        expNotificationMsg <- "Item successfully deleted."
        checkAzureConnNotifMsg "" ""
        if expNotificationMsg <> notificationMessage then
            failwithf " Fail : Incorrect Notification  : %A " notificationMessage

let all _ = 
    negativeTests()
    platformTests() 
    azureRegTests()
    multipleConnectors()

