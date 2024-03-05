function onLoad() {
    var formContext = Xrm.Page;
	
	var salesOrderd = formContext.getAttribute("crbf2_salesorderid").getValue();
	
	Xrm.WebApi.retrieveRecord("salesorder", salesOrderd, "?$select=crbf2_state").then(
		function success(result) {
			if (result.crbf2_state !== 3) {
				disabledAllField()
			}
		},
		function (error) {
			console.log(error.message);
		}
	);
	var formType = formContext.ui.getFormType();
	if (formType == 1) {
		formContext.getAttribute("crbf2_qtysales").setValue(0);
		formContext.getAttribute("crbf2_qtydelivered").setValue(0);
		formContext.getAttribute("crbf2_price").setValue(0);
		formContext.getAttribute("crbf2_totalamountbeforediscount").setValue(0);
		formContext.getAttribute("crbf2_discountamount").setValue(0);
		formContext.getAttribute("crbf2_totalnetamount").setValue(0);
	}
}

function disabledAllField(){
	var formContext = Xrm.Page;
	
	formContext.ui.controls.forEach(function (control, index) {
		control.setDisabled(true)
	});
}