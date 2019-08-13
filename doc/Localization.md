# Localization of eduVPN and Let's Connect! Clients for Windows

eduVPN and Let's Connect! clients are the same client targeted for a different audience. They share the code base and string resources. Therefore, the remainder of this document will reference "eduVPN" only for readability.

1. [eduVPN at Transifex](https://www.transifex.com/amebis/eduvpn/)

   Though many terms are error messages and they never appear for lucky users, we do encourage you to translate them. The unlucky users might be grateful for better understanding the error message.

2. Setup

   - MSI packages [eduVPN.wxl at GitHub](https://github.com/Amebis/eduVPN/blob/master/eduVPN.wxl)
   - EXE bundle [thm.wxl at GitHub](https://github.com/Amebis/eduVPN/blob/master/Install/thm.wxl)
   - Start menu shortcut and ARP texts [Resources.rc at GitHub](https://github.com/Amebis/eduVPN/blob/master/eduVPN.Resources/Resources.rc) <sup>[1]</sup>
   - OpenVPN Interactive Service entries in SCM [Resources.rc at GitHub](https://github.com/Amebis/eduVPN/blob/master/OpenVPN.Resources/Resources.rc) <sup>[1]</sup>


## General Guidelines

Please use the Windows "official" translations for terms published at [Microsoft Language Portal](https://www.microsoft.com/en-us/language). This will provide a consistent terminology experience on Windows.

Should you need any assistance to get started, please do not hesitate to contact project maintainer at [simon.rozman@amebis.si](mailto:simon.rozman@amebis.si).


## Notes

[1]: #footnote1
<a name="footnote1">[1]</a> The Resource.rc files have only a few terms, therefore we kindly invite you to translate both Resource.rc files. You can find the texts inside the file by following the "LANGUAGE" markers. Just [e-mail us](mailto:simon.rozman@amebis.si) the translations – we can handle the rest.
