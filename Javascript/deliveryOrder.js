function onLoad() {
    var formContext = Xrm.Page;
	disabledAllField();
	
	var formType = formContext.ui.getFormType();
	formContext.getControl("crbf2_state").setDisabled(false);
	if (formType == 1) {
		var transactionDate = formContext.getAttribute("crbf2_transactiondate");
		transactionDate.setValue(new Date());
		formContext.getControl("crbf2_handling").setDisabled(true);
	} else if (formType == 2) {
		formContext.getControl("crbf2_handling").setDisabled(false);
	}
}


function onStateChange() {
	disabledAllField()
}


function disabledAllField(){
	var formContext = Xrm.Page;
	var stateValue = formContext.getAttribute("crbf2_state").getValue();
	
	// disabled all fields when handling is release
	formContext.ui.controls.forEach(function (control, index) {
		if (control.getName() !== "crbf2_state") {
			control.setDisabled(stateValue == 2 || stateValue == 3);
		}
	});
}