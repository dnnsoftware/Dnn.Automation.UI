module DnnMvcSpa

open canopy

/// <summary>MVC Module - Create a new contact. Email created is firstname.lastname@test.com.
/// Twitter handle created is @firstname.lastname. Phone is 555-555-5555.</summary>
/// <param name="moduleid">Module ID of the MVC module.</param>
/// <param name="firstname">First Name of new contact to be created.</param>
/// <param name="lastname">Last Name of new contact to be created.</param>
let addNewContactMVC moduleid firstname lastname =
    openEditMode()
    let handleSelector = sprintf "div.DnnModule-%s>div.dnnDragHint" (moduleid.ToString())
    let pencilIconSelector = sprintf "//div[@id='moduleActions-%s']/ul/li[1]/a/i" (moduleid.ToString())
    let addNewLinkSelector = sprintf "//div[@id='moduleActions-%s']/ul/li[1]/ul/li/a" (moduleid.ToString())
    let openAddContactDialog()= 
        try
            scrollTo handleSelector
            sleep 0.5 //for page to settle down
            hoverOver handleSelector
            waitForElementPresent pencilIconSelector
            hoverOver pencilIconSelector
            waitForElementPresent pencilIconSelector
            clickDnnPopupLink addNewLinkSelector
            true
        with _ ->
            reloadPage()
            waitForElementPresent handleSelector
            false
    let openSucess = retryWithWait 3 0.5 openAddContactDialog
    if not openSucess then
        failwith "  FAIL: MVC Module's Add Contact dialog could not be opened."
    //enter form values
    "#FirstName" << firstname
    "#LastName" << lastname
    let emailAddress = firstname + "." + lastname + "@test.com"
    "#Email" << emailAddress
    "#Phone" << "555-555-5555"
    "#Twitter" << "@" + firstname + "." + lastname
    click "#btnSave"
    waitForSpinnerDone()
    waitPageLoad()
    closeEditMode()
    let rightArrow = "a>i.fa-arrow-right"
    try
        waitForElementPresent rightArrow
    with _ -> ()
    while existsAndVisible rightArrow do
        click rightArrow
        waitPageLoad()
    let emailSelector = sprintf "//div[@class='contactCard']/div[2]/span[.='%s']" emailAddress
    if not(existsAndVisible emailSelector) then
        failwithf "  FAIL: A new contact with name %s %s could not be added in MVC module." firstname lastname

/// <summary>MVC Module - Check the Partial View Call and JSON Call change the time. A page with MVC module deployed should already be open.</summary>
/// <param name="moduleid">Module ID of the MVC module.</param>
let checkPartialAndJsonCallsChangeTimeMVC moduleid =
    let partialViewCallTimeSelector = sprintf "//div[@id='partialupdates%s']/div[4]" (moduleid.ToString())
    let jsonCallTimeSelector = sprintf "//div[@id='jsonupdates%s']/div[4]/span" (moduleid.ToString())
    let partialViewTime = (element partialViewCallTimeSelector).Text
    let jsonTime = (element jsonCallTimeSelector).Text
    sleep 11 //The time updates every 10 secs
    let newPartialViewTime = (element partialViewCallTimeSelector).Text
    let newJsonTime = (element jsonCallTimeSelector).Text
    let mutable failed = false
    let mutable failReasons = "  FAIL: "
    if newPartialViewTime=partialViewTime then
        failed <- true
        failReasons <- failReasons + "Partial View Call did not change the time in MVC Module. "
    if newJsonTime=jsonTime then
        failed <- true
        failReasons <- failReasons + "JSON Call did not change the time in MVC Module. "
    if failed then failwith failReasons

/// <summary>SPA Module - Create a new contact. Email created is firstname.lastname@test.com.
/// Twitter handle created is @firstname.lastname. Phone is 555-555-5555.</summary>
/// <param name="moduleId">Module ID of the MVC module.</param>
/// <param name="firstName">First Name of new contact to be created.</param>
/// <param name="lastName">Last Name of new contact to be created.</param>
let addNewContactSPA moduleId firstName lastName =
    openEditMode()
    let handleSelector = sprintf "div.DnnModule-%s>div.dnnDragHint" (moduleId.ToString())
    let caretDownSelector = sprintf "//div[@id='moduleActions-%s']/ul/li[4]/a/i" (moduleId.ToString())
    let addNewBoxSelector = "//label[contains(text(),'Allow creation')]/../span/span/img"
    let doChangeSetting()=
        try
            scrollTo handleSelector
            sleep 0.5 //for page to settle down
            hoverOver handleSelector
            waitForElementPresent caretDownSelector
            hoverOver caretDownSelector
            waitForElementPresent addNewBoxSelector
            click addNewBoxSelector
            click "div.qsFooter>a.primarybtn"
            waitForAjax()
            closeEditMode()
            true
        with _ ->
            reloadPage()
            waitForElementPresent handleSelector
            false
    let settingChanged = retryWithWait 3 0.5 doChangeSetting
    if not settingChanged then
        failwith "  FAIL: SPA Module's Add Contact setting could not be changed."
    let addContactBtn = "//a[.='Add New Contact']"
    waitForElementPresent addContactBtn
    click addContactBtn
    waitForAjax()
    "//div[contains(@class,'editContact')]/div[1]/input" << firstName
    "//div[contains(@class,'editContact')]/div[2]/input" << lastName
    //Bug DNN-8767 logged for SPA module not accepting long email addresses
    //let emailAddress = firstName + "." + lastName + "@test.com"
    let emailAddress = firstName + "@testing.com"
    "//div[contains(@class,'editContact')]/div[3]/input" << emailAddress
    "//div[contains(@class,'editContact')]/div[4]/input" << "555-555-5555"
    "//div[contains(@class,'editContact')]/div[5]/input" << "@" + firstName + "." + lastName
    click "//a[.='Save']"
    waitForAjax()
    waitForElementPresent "div.contactCard"    
    let emailSelector = sprintf "//div[@class='contactCard']/div[3]/span[.='%s']" emailAddress
    if not(existsAndVisible emailSelector) then
        let disabledRightBtn = "a.disabled>i.fa-angle-right"
        while not(existsAndVisible disabledRightBtn) do
            click "a>i.fa-angle-right" //right button
            waitForAjax()
        if not(existsAndVisible emailSelector) then
            failwithf "  FAIL: A new contact with name %s %s could not be added in SPA module." firstName lastName
