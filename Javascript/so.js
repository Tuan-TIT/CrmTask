function onLoad() {
    var formContext = Xrm.Page;
	disabledAllField();
	
	var formType = formContext.ui.getFormType();
	if (formType == 1) {
		var transactionDate = formContext.getAttribute("crbf2_transactiondate");
		transactionDate.setValue(new Date());
		formContext.getControl("crbf2_handling").setDisabled(true);
		formContext.getAttribute("crbf2_grandtotal").setValue(0);
	} else if (formType == 2) {
		formContext.getControl("crbf2_handling").setDisabled(false);
	}
}


function onHandlingChange() {
	disabledAllField()
}


function disabledAllField(){
	var formContext = Xrm.Page;
	var handlingValue = formContext.getAttribute("crbf2_handling").getValue();
	
	// disabled all fields when handling is release
	formContext.ui.controls.forEach(function (control, index) {
		if (control.getName() !== "crbf2_handling") {
			control.setDisabled(handlingValue == 2 || handlingValue == 3);
		}
	});
}