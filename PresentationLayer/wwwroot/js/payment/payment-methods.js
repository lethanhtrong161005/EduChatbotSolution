document.addEventListener(
    "DOMContentLoaded",
    () => {
        const paymentCards =
            document.querySelectorAll(
                ".payment-option");

        const selectedMethodInput =
            document.getElementById(
                "SelectedMethod");

        const summaryMethod =
            document.getElementById(
                "summaryPaymentMethod");

        const continueButton =
            document.getElementById(
                "continueButton");

        paymentCards.forEach(card => {
            card.addEventListener(
                "click",
                () => {
                    paymentCards.forEach(
                        x => x.classList.remove(
                            "selected"));

                    card.classList.add(
                        "selected");

                    const radio =
                        card.querySelector(
                            "input[type='radio']");

                    radio.checked = true;

                    const methodCode =
                        radio.value;

                    const displayName =
                        card.querySelector(
                            ".payment-name")
                            .textContent
                            .trim();

                    selectedMethodInput.value =
                        methodCode;

                    summaryMethod.textContent =
                        displayName;

                    continueButton.disabled =
                        false;
                });
        });
    });
