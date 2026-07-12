import os
import re

account_dir = r"PresentationLayer\Pages\Account"
files_to_process = [
    "Login.cshtml", "Register.cshtml", "ForgotPassword.cshtml",
    "ResetPassword.cshtml", "VerifyEmail.cshtml", "VerifyEmailUpdate.cshtml",
    "ForceChangePassword.cshtml"
]

for filename in files_to_process:
    filepath = os.path.join(account_dir, filename)
    if not os.path.exists(filepath):
        continue
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Replace Layout = null with ViewData["NoContainer"]
    content = re.sub(r'Layout\s*=\s*null;.*', r'ViewData["NoContainer"] = true;', content)

    # 2. Extract unique styles and scripts from head (we know they use account-auth.css or specific css)
    # We will just manually add the section Styles since we know what they need.
    
    # 3. Remove <html> to <body>
    content = re.sub(r'<!DOCTYPE html>.*?<body>', r'@section Styles {\n    <link rel="stylesheet" href="~/css/account-auth.css">\n    <link rel="stylesheet" href="~/css/account-verify-email.css">\n}', content, flags=re.DOTALL)
    
    # 4. Remove </body></html>
    content = re.sub(r'</body>\s*</html>', '', content)
    
    # Write back
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

print("Processed all files.")
