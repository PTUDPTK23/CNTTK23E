(() => {
  const dialog = document.querySelector('[data-confirm-dialog]');
  if (!dialog || typeof dialog.showModal !== 'function') return;
  let pendingForm = null;
  document.querySelectorAll('[data-confirm-form]').forEach((form) => {
    form.addEventListener('submit', (event) => {
      if (form.dataset.confirmed === 'true') return;
      event.preventDefault(); pendingForm = form;
      dialog.querySelector('[data-confirm-heading]').textContent = form.dataset.confirmTitle;
      dialog.querySelector('[data-confirm-copy]').textContent = form.dataset.confirmMessage;
      dialog.showModal();
    });
  });
  dialog.addEventListener('close', () => {
    if (dialog.returnValue === 'confirm' && pendingForm) {
      pendingForm.dataset.confirmed = 'true'; pendingForm.requestSubmit();
    }
    pendingForm = null;
  });
})();
