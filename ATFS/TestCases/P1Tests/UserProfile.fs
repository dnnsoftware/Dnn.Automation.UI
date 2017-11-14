module UserProfile

open System.IO
open canopy
open DnnCanopyContext
open DnnAddToRole
open DnnUserProfile
open DnnUserLogin

/// <summary>Verifies a Profile Property against an expected value</summary>
/// <param name="profName">Name of the Profile Property</param>
/// <param name="elm">Selector of the UI element to test</param>
/// <param name="expValue">Expected value of the property</param>
let private testProfileValue profName elm expValue = 
    let actualValue = (element elm).GetAttribute("value")
    if actualValue <> expValue then
        failwithf "  FAIL: %s update failed in User Profile. Expected %A, actual was %A." profName expValue actualValue

let private  avatarTests _ =

    context "User Profile: Profile Avatar Tests"

    "User Profile | Regular User | Add Profile Picture" @@@ fun _ ->
        logOff()
        loginAsRegularUser() |> ignore
        let origPP = getProfilePictureSrc()
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        //add profile picture
        let filePath = Path.Combine(additionalFilesLocation, "altlogo.png")
        addProfilePicture filePath       
        //verify profile picture changed
        goto "/"
        if getProfilePictureSrc() = origPP then
            failwith "  Fail: Profile Picture has not changed after uploading new image"
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false

    "User Profile | Regular User | Update Profile Picture Visibility" @@@ fun _ ->
        loginAsRegularUser() |> ignore
        let profPicSrc = getProfilePictureSrc()
        let wc = new System.Net.WebClient()
        let publicImgPath = @"C:\temp\public_DIH_Image.png"
        let privateImgPath = @"C:\temp\private_DIH_Image.png"
        let deleteFiles() =
            try
                (FileInfo publicImgPath).Delete()
            with _ -> ()
            try
                (FileInfo privateImgPath).Delete()
            with _ -> ()
        deleteFiles()
        //download image from DnnImageHandler link as an anonymous user
        logOff()      
        wc.DownloadFile(profPicSrc, publicImgPath)
        //set image visibility to Admin only
        loginAsRegularUser() |> ignore
        openProfilePicSection()
        let visDdn = "//div[contains(@id,'ProfileProperties_Photo')]/div/div/div[@class='dnnButtonArrow']"
        click visDdn
        let adminOption = visDdn + "/../../ul/li[3]/span/span/img"
        waitClick adminOption
        updateProfile()
        //download image from DnnImageHandler link as an anonymous user
        logOff()
        wc.DownloadFile(profPicSrc, privateImgPath)
        //compare images
        let mutable testFailed = false
        if File.ReadAllBytes(publicImgPath) = File.ReadAllBytes(privateImgPath) then testFailed <- true     
        //delete images
        deleteFiles()
        if testFailed then
            failwith "  Fail: Anonymous user is able to view profile picture whose visibility is restricted."

let private otherTests _ =

    context "User Profile: Profile Update Tests"

    "User Profile | Regular User | Update Display Name and Email Address" @@@ fun _ ->
        logOff()
        let ru = loginAsRegularUser()
        closePersonaBarIfOpen()
        gotoEditProfilePage()        
        updateAccountProperties "Regular User" "reguser@test.com"
        //check values
        reloadPage()
        waitClick "//a[contains(text(),'Manage Account')]"
        waitForAjax()
        expandManageAccountPage()
        let displayNameTB = "#dnn_ctr_EditUser_userForm_displayName_displayName_TextBox"
        waitForElementPresent displayNameTB
        testProfileValue "Display Name" displayNameTB "Regular User"
        testProfileValue "Email Address" "#dnn_ctr_EditUser_userForm_email_email_TextBox" "reguser@test.com"
        //set display name and email address back to original values
        //updateAccountProperties ru.DisplayName ru.EmailAddress

    "User Profile | Regular User | Update First and Last Name, Address, City, and Phone Number" @@@ fun _ ->
        loginAsRegularUser() |> ignore
        gotoEditProfilePage()
        updateProfileProperties "TestFirstName" "TestLastName" "9440 202nd Street" "Langley" "604-555-5555"
        //check values
        goto "/"
        gotoEditProfilePage()
        click "//a[contains(text(),'Manage Profile')]"
        waitForAjax()
        expandManageProfilePage()
        testProfileValue "First Name" "#dnn_ctr_EditUser_Profile_ProfileProperties_FirstName_FirstName" "TestFirstName"
        testProfileValue "Last Name" "#dnn_ctr_EditUser_Profile_ProfileProperties_LastName_LastName" "TestLastName"
        let streetTB = "#dnn_ctr_EditUser_Profile_ProfileProperties_Street_Street"
        scrollTo streetTB
        testProfileValue "Street Address" streetTB "9440 202nd Street"
        testProfileValue "City" "#dnn_ctr_EditUser_Profile_ProfileProperties_City_City" "Langley"
        testProfileValue "Phone Number" "#dnn_ctr_EditUser_Profile_ProfileProperties_Cell_Cell" "604-555-5555"

let all _ =
    avatarTests()
    otherTests()
