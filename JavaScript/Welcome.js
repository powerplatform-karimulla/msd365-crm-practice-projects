function showResultPopup(executionContext) {
    var formContext = executionContext.getFormContext();

    setTimeout(function () {

        var resultField = formContext.getAttribute("shaik_result");
        if (resultField == null) return;

        var resultValue = resultField.getValue();

        if (resultValue === "Pass") {
            Xrm.Navigation.openAlertDialog({
                title: "🎉 Result Announced",
                text: "Congratulations! You are Passed!\nAll subjects secured >= 35 marks."
            });
        } else if (resultValue === "Fail") {
            Xrm.Navigation.openAlertDialog({
                title: "❌ Result Announced",
                text: "Sorry! You are Failed.\nBetter luck next time!\nCheck all subject marks — each must be >= 35."
            });
        }

    }, 2000);
}