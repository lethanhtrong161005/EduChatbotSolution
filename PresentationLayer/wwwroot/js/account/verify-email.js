'use strict';

(function () {
    // ── OTP digit inputs ──────────────────────────────────────────
    const digits = Array.from(document.querySelectorAll('.otp-digit'));
    const hiddenCode = document.getElementById('hiddenCode');

    digits.forEach((input, index) => {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace') {
                if (!input.value && index > 0) {
                    digits[index - 1].focus();
                }
                input.value = '';
                syncHiddenCode();
                return;
            }

            // Allow only digits
            if (!/^\d$/.test(e.key) && !['ArrowLeft', 'ArrowRight', 'Tab'].includes(e.key)) {
                e.preventDefault();
            }
        });

        input.addEventListener('input', () => {
            // Keep only the last digit typed
            const digit = input.value.replace(/\D/g, '').slice(-1);
            input.value = digit;

            if (digit) {
                input.classList.add('otp-digit--filled');
                if (index < digits.length - 1) {
                    digits[index + 1].focus();
                }
            } else {
                input.classList.remove('otp-digit--filled');
            }

            syncHiddenCode();
        });

        // Paste handling on any digit box
        input.addEventListener('paste', (e) => {
            e.preventDefault();
            const text = (e.clipboardData || window.clipboardData)
                .getData('text')
                .replace(/\D/g, '')
                .slice(0, 6);

            text.split('').forEach((ch, i) => {
                if (digits[i]) {
                    digits[i].value = ch;
                    digits[i].classList.add('otp-digit--filled');
                }
            });

            const nextEmpty = digits.findIndex(d => !d.value);
            if (nextEmpty !== -1) digits[nextEmpty].focus();
            else digits[digits.length - 1].focus();

            syncHiddenCode();
        });
    });

    function syncHiddenCode() {
        if (hiddenCode) {
            hiddenCode.value = digits.map(d => d.value).join('');
        }
    }

    // ── Form submit guard ─────────────────────────────────────────
    const verifyForm = document.getElementById('verifyForm');
    let isSubmitting = false;

    verifyForm?.addEventListener('submit', (e) => {
        syncHiddenCode();

        if (isSubmitting) {
            e.preventDefault();
            return;
        }

        const code = hiddenCode?.value ?? '';
        if (!/^\d{6}$/.test(code)) {
            e.preventDefault();
            markDigitsError();
            return;
        }

        isSubmitting = true;
        const btn = document.getElementById('verifyBtn');
        if (btn) {
            btn.disabled = true;
            btn.querySelector('.btn-text').textContent = 'Verifying…';
        }
    });

    function markDigitsError() {
        digits.forEach(d => {
            d.classList.add('otp-digit--error');
            setTimeout(() => d.classList.remove('otp-digit--error'), 600);
        });
    }

    // ── Countdown timer ───────────────────────────────────────────
    const timerEl = document.getElementById('countdownDisplay');
    const timerChip = document.getElementById('countdownChip');
    const resendBtn = document.getElementById('resendBtn');

    let secondsLeft = parseInt(timerEl?.dataset.secondsRemaining ?? '180', 10);
    let countdownInterval = null;

    function startCountdown(seconds) {
        secondsLeft = seconds;
        if (timerChip) timerChip.classList.remove('countdown-chip--expired');
        if (resendBtn) resendBtn.disabled = true;

        clearInterval(countdownInterval);
        countdownInterval = setInterval(() => {
            secondsLeft--;
            updateTimerDisplay();

            if (secondsLeft <= 0) {
                clearInterval(countdownInterval);
                onCountdownExpired();
            }
        }, 1000);

        updateTimerDisplay();
    }

    function updateTimerDisplay() {
        if (!timerEl) return;
        const m = String(Math.floor(secondsLeft / 60)).padStart(2, '0');
        const s = String(secondsLeft % 60).padStart(2, '0');
        timerEl.textContent = `${m}:${s}`;
    }

    function onCountdownExpired() {
        if (timerChip) timerChip.classList.add('countdown-chip--expired');
        if (resendBtn) resendBtn.disabled = false;
    }

    // Start immediately on page load
    if (secondsLeft > 0) {
        startCountdown(secondsLeft);
    } else {
        onCountdownExpired();
    }

    // ── Resend code (AJAX) ────────────────────────────────────────
    const resendForm = document.getElementById('resendForm');

    resendForm?.addEventListener('submit', async (e) => {
        e.preventDefault();

        if (resendBtn) resendBtn.disabled = true;

        const formData = new FormData(resendForm);

        try {
            const response = await fetch(resendForm.action, {
                method: 'POST',
                body: formData,
            });

            const data = await response.json();

            if (data.success) {
                showToast('A new verification code has been sent to your email.', 'success');
                startCountdown(data.remainingSeconds ?? 180);
            } else {
                showToast(data.error ?? 'Failed to resend code. Please try again.', 'error');
                if (resendBtn) resendBtn.disabled = false;
            }
        } catch {
            showToast('Network error. Please check your connection and try again.', 'error');
            if (resendBtn) resendBtn.disabled = false;
        }
    });

    // ── Toast helper ──────────────────────────────────────────────
    function showToast(message, type = 'success') {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast toast--${type}`;
        toast.innerHTML = `<i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i> ${message}`;
        container.appendChild(toast);

        setTimeout(() => toast.remove(), 3200);
    }

    // ── Bubble animation (matches login/register pages) ───────────
    const count = window.innerWidth > 768 ? 5 : 2;
    for (let i = 0; i < count; i++) {
        const bubble = document.createElement('div');
        bubble.className = 'bubble';
        const size = Math.random() * 60 + 30;
        bubble.style.cssText = [
            `width:${size}px`,
            `height:${size}px`,
            `left:${Math.random() * 100}%`,
            `top:${Math.random() * 100}%`,
            `--float-duration:${Math.random() * 10 + 8}s`,
        ].join(';');
        document.querySelector('.bg-animation')?.appendChild(bubble);
    }
})();
