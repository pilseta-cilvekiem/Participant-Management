import { formatGuid, getLocalizedText, performActionFromForm, performActionFromGrid } from "./lib/common";

export async function processFromForm(form: Xrm.PageBase<Xrm.AttributeCollectionBase, Xrm.TabCollectionBase, Xrm.ControlCollectionBase>) {
    await performActionFromForm(form, process, "ProcessingTransaction");
}

export async function processFromGrid(grid: Xrm.SubGridControl<any>, selectedIds: string[]) {
    await performActionFromGrid(grid, selectedIds, process, async i => await getLocalizedText("ProcessingTransactions", i, selectedIds.length));
}

async function process(id: string) {
    await XrmQuery.promiseRequest("POST", `pc_transactions(${formatGuid(id)})/Microsoft.Dynamics.CRM.pc_ProcessTransaction`, null);
}

export async function markAsNonPaymentFromForm(form: Xrm.PageBase<Xrm.AttributeCollectionBase, Xrm.TabCollectionBase, Xrm.ControlCollectionBase>) {
    await performActionFromForm(form, markAsNonPayment, "MarkingTransactionAsNonPayment");
}

export async function markAsNonPaymentFromGrid(grid: Xrm.SubGridControl<any>, selectedIds: string[]) {
    await performActionFromGrid(grid, selectedIds, markAsNonPayment, async i => await getLocalizedText("MarkingTransactionsAsNonPayment", i, selectedIds.length));
}

async function markAsNonPayment(id: string) {
    await XrmQuery.promiseRequest("POST", `pc_transactions(${formatGuid(id)})/Microsoft.Dynamics.CRM.pc_MarkTransactionAsNonPayment`, null);
}

export async function clearNonPaymentAmountFromForm(form: Xrm.PageBase<Xrm.AttributeCollectionBase, Xrm.TabCollectionBase, Xrm.ControlCollectionBase>) {
    await performActionFromForm(form, clearNonPaymentAmount, "ClearingNonPaymentAmount");
}

async function clearNonPaymentAmount(id: string) {
    await XrmQuery.promiseRequest("POST", `pc_transactions(${formatGuid(id)})/Microsoft.Dynamics.CRM.pc_ClearNonPaymentAmount`, null);
}
