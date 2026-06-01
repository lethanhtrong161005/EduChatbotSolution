document.addEventListener('DOMContentLoaded', () => {

    let selectedPlanId = null;
    let selectedOptionId = null;

    const continueButton =
        document.getElementById('continueButton');

    const purchaseForm =
        document.getElementById('purchaseForm');

    document
        .querySelectorAll('.plan-option-btn')
        .forEach(button => {

            button.addEventListener('click', () => {

                document
                    .querySelectorAll('.plan-option-btn')
                    .forEach(x => x.classList.remove('selected'));

                button.classList.add('selected');

                selectedPlanId =
                    button.dataset.planId;

                selectedOptionId =
                    button.dataset.optionId;

                const card =
                    button.closest('.plan-card');

                const planName =
                    card.querySelector('.plan-name')
                        .textContent
                        .trim();

                document.getElementById(
                    'selectedOptionId'
                ).value = selectedOptionId;

                document.getElementById(
                    'selectedPlan'
                ).textContent = planName;

                document.getElementById(
                    'selectedOption'
                ).textContent =
                    button.dataset.optionName;

                document.getElementById(
                    'selectedDuration'
                ).textContent =
                    `${button.dataset.duration} Days`;

                document.getElementById(
                    'selectedPrice'
                ).textContent =
                    `${Number(
                        button.dataset.price
                    ).toLocaleString()}₫`;

                continueButton.disabled = false;
            });
        });

    purchaseForm.addEventListener(
        'submit',
        (e) => {
            e.preventDefault();

            if (!selectedOptionId)
                return;
        });
});
