document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll(".password-toggle").forEach((button) => {
    button.addEventListener("click", () => {
      const wrapper = button.closest(".password-wrap");
      const input = wrapper?.querySelector("input");
      const icon = button.querySelector("i");

      if (!input || !icon) {
        return;
      }

      const isPassword = input.type === "password";
      input.type = isPassword ? "text" : "password";
      icon.classList.toggle("fa-eye", !isPassword);
      icon.classList.toggle("fa-eye-slash", isPassword);
      button.setAttribute("aria-label", isPassword ? "Приховати пароль" : "Показати пароль");
    });
  });

  const helpModal = document.getElementById("helpModal");
  const helpModalTitle = document.getElementById("helpModalTitle");
  const helpModalBody = document.getElementById("helpModalBody");

  if (helpModal && helpModalTitle && helpModalBody && window.bootstrap) {
    const modal = bootstrap.Modal.getOrCreateInstance(helpModal);

    document.querySelectorAll("[data-help]").forEach((button) => {
      button.addEventListener("click", () => {
        helpModalTitle.textContent = button.dataset.helpTitle || "Підказка";
        helpModalBody.textContent = button.dataset.help || "";
        modal.show();
      });
    });
  }

  document.querySelectorAll(".image-upload-input").forEach((input) => {
    input.addEventListener("change", () => {
      const wrapper = input.closest(".image-upload-control");
      const fileName = wrapper?.querySelector(".image-upload-name");

      if (!fileName) {
        return;
      }

      fileName.textContent = input.files?.[0]?.name || fileName.dataset.emptyText || "Файл не вибрано";
    });
  });
});
