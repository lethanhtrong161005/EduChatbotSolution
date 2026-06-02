document.addEventListener(
    "DOMContentLoaded",
    () => {
        const root =
            document.querySelector(
                ".payment-processing-page");

        const paymentId =
            root.dataset.paymentId;

        const statusText =
            document.getElementById(
                "statusText");

        async function checkStatus() {
            try {
                const response =
                    await fetch(
                        `/payment/status/${paymentId}`);

                if (!response.ok)
                    return;

                const result =
                    await response.json();

                switch (result.status) {
                    case "Pending":

                        statusText.textContent =
                            "Waiting for provider confirmation...";
                        break;

                    case "Fulfilled":

                        statusText.textContent =
                            "Payment confirmed. Activating subscription...";

                        setTimeout(
                            () => {
                                window.location.href =
                                    result.redirectUrl;
                            },
                            1500);

                        break;

                    case "Failed":

                        statusText.textContent =
                            "Payment verification failed.";

                        break;

                    case "Cancelled":

                        statusText.textContent =
                            "Payment was cancelled.";

                        break;
                }
            }
            catch {
            }
        }

        document
            .getElementById(
                "refreshButton")
            .addEventListener(
                "click",
                checkStatus);

        setInterval(
            checkStatus,
            5000);
    });
