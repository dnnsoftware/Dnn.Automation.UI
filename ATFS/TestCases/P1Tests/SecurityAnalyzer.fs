module SecurityAnalyzer

open DnnCanopyContext
open DnnUserLogin
open DnnSecurityAnalyzer

let basicTests _ =
    context "Security Analyzer Tests"

    "Security Analyzer | Host | Verify Audit Checks" @@@ fun _ ->
        loginAsHost()
        openPBSecurity()
        let failedReasons = testSecurityAnalyzer 1
        if failedReasons <> "" then failwithf "  FAIL: Security Analyzer: Audit Checks\n%s" failedReasons

    "Security Analyzer | Host | Verify Scanner Check and SuperUser Activity" @@@ fun _ ->
        let failedReasons = testSecurityAnalyzer 2
        if failedReasons <> "" then failwithf "  FAIL: Security Analyzer: Scanner Check and SuperUser Activity\n%s" failedReasons

let all _ =
    basicTests()
        