# Localization of eduVPN Client for Windows


## The minimal set of resources to translate

1. [eduVPN at POEditor](https://poeditor.com/join/project/0cJKTOUjzn)
2. [eduOAuth at POEditor](https://poeditor.com/join/project/KluhCNBAWP) (terms not starting with "Error…")
3. Setup
   - MSI packages [eduVPN.wxl at GitHub](https://github.com/Amebis/eduVPN/blob/master/eduVPN.wxl) and [LetsConnect.wxl at GitHub](https://github.com/Amebis/eduVPN/blob/master/LetsConnect.wxl)
   - EXE bundle [thm.wxl at GitHub](https://github.com/Amebis/eduVPN/blob/master/Install/thm.wxl)
   - Start menu shortcut and ARP texts [Resources.rc at GitHub](https://github.com/Amebis/eduVPN/blob/master/eduVPN.Resources/Resources.rc) <sup>[1]</sup>


## Optional set of resources to translate

1. Setup
   - OpenVPN Interactive Service entries in SCM [Resources.rc at GitHub](https://github.com/Amebis/eduVPN/blob/master/OpenVPN.Resources/Resources.rc) <sup>[1]</sup>
2. [eduOpenVPN at POEditor](https://poeditor.com/join/project/tkC9Zd0HXN)
3. [eduOAuth at POEditor](https://poeditor.com/join/project/KluhCNBAWP) ("Error..." terms)
4. [eduJSON at POEditor](https://poeditor.com/join/project/0QH1bswu6J) <sup>[2]</sup>
5. [eduEd25519 at POEditor](https://poeditor.com/join/project/O7iLbVa1l6) <sup>[2]</sup>

Though many terms are error messages and they never appear for lucky users, we do encourage you to translate them. The unlucky users might be grateful for better understanding the error message.


## General Guidelines

Please use the Windows "official" translations for terms published at [Microsoft Language Portal](https://www.microsoft.com/en-us/language). This will provide a consistent terminology experience on Windows.

Should you need any assistance to get started, please do not hesitate to contact project maintainer at [simon.rozman@amebis.si](mailto:simon.rozman@amebis.si).


## Notes

[1]: #footnote1
<a name="footnote1">[1]</a> The Resource.rc files have only a few terms, therefore we kindly invite you to translate both Resource.rc files. You can find the texts inside the file by following the "LANGUAGE" markers. Just [e-mail us](mailto:simon.rozman@amebis.si) the translations – we can handle the rest.

[2]: #footnote2
<a name="footnote2">[2]</a> Those libraries are so low-level their errors have very low probability to occur in the real world. Fortunately, they do not have much text to translate.
