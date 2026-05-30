/**
 * EduChatAI - Register Page Interactions
 * Handles form submission, loading state, password toggle,
 * background bubble animation, and double-submit prevention.
 */

document.addEventListener('DOMContentLoaded', function () {
    var registerForm = document.getElementById('registerForm');
    var registerBtn = document.getElementById('registerBtn');

    // ── Loading animation on form submit ──────────────────────
    registerForm.addEventListener('submit', function (e) {
        if (registerForm.checkValidity() === false) {
            e.preventDefault();
            e.stopPropagation();
        } else {
            registerBtn.classList.add('loading');
            registerBtn.disabled = true;
        }
    });

    // ── Toggle password visibility ───────────────────────────
    var toggleButtons = document.querySelectorAll('.toggle-password');
    toggleButtons.forEach(function (button) {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            var targetId = this.getAttribute('data-target');
            var passwordInput = document.getElementById(targetId);
            var icon = this.querySelector('i');

            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                icon.classList.remove('fa-eye');
                icon.classList.add('fa-eye-slash');
            } else {
                passwordInput.type = 'password';
                icon.classList.remove('fa-eye-slash');
                icon.classList.add('fa-eye');
            }
        });
    });

    // ── Background bubbles ───────────────────────────────────
    createBubbles();

    // ── Prevent double form submission ───────────────────────
    var isSubmitting = false;
    registerForm.addEventListener('submit', function (e) {
        if (isSubmitting) {
            e.preventDefault();
            return;
        }
        isSubmitting = true;
    });
});

/**
 * Creates animated bubble elements inside the background container.
 * Generates fewer bubbles on mobile for performance.
 */
function createBubbles() {
    var bgAnimation = document.querySelector('.bg-animation');
    if (!bgAnimation) return;

    var bubbleCount = window.innerWidth > 768 ? 5 : 2;

    for (var i = 0; i < bubbleCount; i++) {
        var bubble = document.createElement('div');
        bubble.className = 'bubble';

        var size = Math.random() * 100 + 50;
        bubble.style.width = size + 'px';
        bubble.style.height = size + 'px';

        bubble.style.left = Math.random() * 100 + '%';
        bubble.style.top = Math.random() * 100 + '%';

        bubble.style.setProperty('--float-duration', Math.random() * 10 + 8 + 's');

        bgAnimation.appendChild(bubble);
    }
}
