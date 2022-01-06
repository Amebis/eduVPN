/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Globalization;

namespace eduVPN.Models
{
    /// <summary>
    /// Country
    /// </summary>
    public class Country : IComparable
    {
        #region Fields

        /// <summary>
        /// All known countries with English and local names
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, string>> Countries = new Dictionary<string, Dictionary<string, string>>()
        {
            { "AD", new Dictionary<string, string>() {
                { "ca", "Andorra" },
                { "en", "Andorra" }
            }},
            { "AE", new Dictionary<string, string>() {
                { "ar", "دولة الإمارات العربيّة المتّحدة" },
                { "en", "United Arab Emirates" }
            }},
            { "AF", new Dictionary<string, string>() {
                { "en", "Afghanistan" },
                { "fa", "د افغانستان اسلامي دولتدولت اسلامی افغانستان" },
                { "ps", "جمهوری اسلامی افغانستان" }
            }},
            { "AG", new Dictionary<string, string>() {
                { "en", "Antigua and Barbuda" }
            }},
            { "AI", new Dictionary<string, string>() {
                { "en", "Anguilla" }
            }},
            { "AL", new Dictionary<string, string>() {
                { "en", "Albania" },
                { "sq", "Shqipëria" }
            }},
            { "AM", new Dictionary<string, string>() {
                { "en", "Armenia" },
                { "hy", "Հայաստան" }
            }},
            { "AO", new Dictionary<string, string>() {
                { "en", "Angola" },
                { "pt", "Angola" }
            }},
            { "AQ", new Dictionary<string, string>() {
                { "en", "Antarctica" },
                { "es", "Antártico" },
                { "fr", "Antarctique" },
                { "ru", "Антарктике" }
            }},
            { "AR", new Dictionary<string, string>() {
                { "en", "Argentina" },
                { "es", "Argentina" }
            }},
            { "AS", new Dictionary<string, string>() {
                { "en", "American Samoa" }
            }},
            { "AT", new Dictionary<string, string>() {
                { "de", "Österreich" },
                { "en", "Austria" }
            }},
            { "AU", new Dictionary<string, string>() {
                { "da", "Australien" },
                { "en", "Australia" },
                { "nb", "Australia" },
                { "nl", "Australië" }
            }},
            { "AW", new Dictionary<string, string>() {
                { "en", "Aruba" },
                { "nl", "Aruba" }
            }},
            { "AX", new Dictionary<string, string>() {
                { "en", "Aland Islands" },
                { "sv", "Åland" }
            }},
            { "AZ", new Dictionary<string, string>() {
                { "az", "Azərbaycan" },
                { "en", "Azerbaijan" }
            }},
            { "BA", new Dictionary<string, string>() {
                { "bs", "Bosna i Hercegovina" },
                { "en", "Bosnia and Herzegovina" }
            }},
            { "BB", new Dictionary<string, string>() {
                { "en", "Barbados" }
            }},
            { "BD", new Dictionary<string, string>() {
                { "bn", "গণপ্রজাতন্ত্রী বাংলাদেশ" },
                { "en", "Bangladesh" }
            }},
            { "BE", new Dictionary<string, string>() {
                { "de", "Belgien" },
                { "en", "Belgium" },
                { "fr", "Belgique" },
                { "nl", "België" }
            }},
            { "BF", new Dictionary<string, string>() {
                { "en", "Burkina Faso" },
                { "fr", "Burkina Faso" }
            }},
            { "BG", new Dictionary<string, string>() {
                { "bg", "България" },
                { "en", "Bulgaria" }
            }},
            { "BH", new Dictionary<string, string>() {
                { "ar", "البحرين" },
                { "en", "Bahrain" }
            }},
            { "BI", new Dictionary<string, string>() {
                { "en", "Burundi" },
                { "fr", "Burundi" }
            }},
            { "BJ", new Dictionary<string, string>() {
                { "en", "Benin" },
                { "fr", "Bénin" }
            }},
            { "BL", new Dictionary<string, string>() {
                { "en", "Saint-Barthélemy" },
                { "fr", "Saint-Barthélemy" }
            }},
            { "BM", new Dictionary<string, string>() {
                { "en", "Bermuda" }
            }},
            { "BN", new Dictionary<string, string>() {
                { "en", "Brunei Darussalam" },
                { "ms", "Brunei Darussalam" }
            }},
            { "BO", new Dictionary<string, string>() {
                { "ay", "Wuliwya" },
                { "en", "Bolivia" },
                { "es", "Bolivia" },
                { "gn", "Volívia" },
                { "qu", "Bulibiya" }
            }},
            { "BQ", new Dictionary<string, string>() {
                { "en", "Caribbean Netherlands" },
                { "nl", "Caribisch Nederland" }
            }},
            { "BR", new Dictionary<string, string>() {
                { "en", "Brazil" },
                { "pt", "Brasil" }
            }},
            { "BS", new Dictionary<string, string>() {
                { "en", "Bahamas" }
            }},
            { "BT", new Dictionary<string, string>() {
                { "dz", "འབྲུག་ཡུལ" },
                { "en", "Bhutan" }
            }},
            { "BV", new Dictionary<string, string>() {
                { "en", "Bouvet Island" },
                { "no", "Bouvetøya" }
            }},
            { "BW", new Dictionary<string, string>() {
                { "en", "Botswana" }
            }},
            { "BY", new Dictionary<string, string>() {
                { "be", "Беларусь" },
                { "en", "Belarus" }
            }},
            { "BZ", new Dictionary<string, string>() {
                { "en", "Belize" }
            }},
            { "CA", new Dictionary<string, string>() {
                { "en", "Canada" }
            }},
            { "CC", new Dictionary<string, string>() {
                { "en", "Cocos (Keeling) Islands" }
            }},
            { "CD", new Dictionary<string, string>() {
                { "en", "Democratic Republic of the Congo (Congo-Kinshasa, former Zaire)" },
                { "fr", "République Démocratique du Congo" }
            }},
            { "CF", new Dictionary<string, string>() {
                { "en", "Central African Republic" },
                { "fr", "République centrafricaine" },
                { "sg", "Ködörösêse tî Bêafrîka" }
            }},
            { "CG", new Dictionary<string, string>() {
                { "en", "Republic of the Congo (Congo-Brazzaville)" },
                { "fr", "République du Congo" }
            }},
            { "CH", new Dictionary<string, string>() {
                { "de", "Schweiz" },
                { "en", "Switzerland" },
                { "fr", "Suisse" },
                { "it", "Svizzera" },
                { "rm", "Svizra" }
            }},
            { "CI", new Dictionary<string, string>() {
                { "en", "Ivory Coast" },
                { "fr", "Côte d'Ivoire" }
            }},
            { "CK", new Dictionary<string, string>() {
                { "en", "Cook Islands" },
                { "rar", "Kūki ʻĀirani" }
            }},
            { "CL", new Dictionary<string, string>() {
                { "en", "Chile" },
                { "es", "Chile" }
            }},
            { "CM", new Dictionary<string, string>() {
                { "en", "Cameroon" },
                { "fr", "Cameroun" }
            }},
            { "CN", new Dictionary<string, string>() {
                { "en", "China" },
                { "zh-hans", "中国" }
            }},
            { "CO", new Dictionary<string, string>() {
                { "en", "Colombia" },
                { "es", "Colombia" }
            }},
            { "CR", new Dictionary<string, string>() {
                { "en", "Costa Rica" },
                { "es", "Costa Rica" }
            }},
            { "CU", new Dictionary<string, string>() {
                { "en", "Cuba" },
                { "es", "Cuba" }
            }},
            { "CV", new Dictionary<string, string>() {
                { "en", "Cabo Verde" },
                { "pt", "Cabo Verde" }
            }},
            { "CW", new Dictionary<string, string>() {
                { "en", "Curaçao" },
                { "nl", "Curaçao" }
            }},
            { "CX", new Dictionary<string, string>() {
                { "en", "Christmas Island" }
            }},
            { "CY", new Dictionary<string, string>() {
                { "el", "Κύπρος" },
                { "en", "Cyprus" },
                { "tr", "Kibris" }
            }},
            { "CZ", new Dictionary<string, string>() {
                { "cs", "Česká republika" },
                { "en", "Czech Republic" }
            }},
            { "DE", new Dictionary<string, string>() {
                { "de", "Deutschland" },
                { "en", "Germany" },
                { "nb", "Tyskland" },
                { "nl", "Duitsland" }
            }},
            { "DJ", new Dictionary<string, string>() {
                { "aa", "Gabuutih" },
                { "ar", "جيبوتي" },
                { "en", "Djibouti" },
                { "fr", "Djibouti" },
                { "so", "Jabuuti" }
            }},
            { "DK", new Dictionary<string, string>() {
                { "da", "Danmark" },
                { "en", "Denmark" },
                { "nb", "Danmark" },
                { "nl", "Denemarken" }
            }},
            { "DM", new Dictionary<string, string>() {
                { "en", "Dominica" }
            }},
            { "DO", new Dictionary<string, string>() {
                { "en", "Dominican Republic" },
                { "es", "República Dominicana" }
            }},
            { "DZ", new Dictionary<string, string>() {
                { "ar", "الجزائر" },
                { "en", "Algeria" }
            }},
            { "EC", new Dictionary<string, string>() {
                { "en", "Ecuador" },
                { "es", "Ecuador" }
            }},
            { "EE", new Dictionary<string, string>() {
                { "en", "Estonia" },
                { "et", "Eesti" }
            }},
            { "EG", new Dictionary<string, string>() {
                { "ar", "مصر" },
                { "en", "Egypt" }
            }},
            { "EH", new Dictionary<string, string>() {
                { "ar", "Sahara Occidental" },
                { "en", "Western Sahara" }
            }},
            { "ER", new Dictionary<string, string>() {
                { "ar", "إرتريا" },
                { "en", "Eritrea" },
                { "ti", "ኤርትራ" }
            }},
            { "ES", new Dictionary<string, string>() {
                { "ast", "España" },
                { "en", "Spain" }
            }},
            { "ET", new Dictionary<string, string>() {
                { "am", "ኢትዮጵያ" },
                { "en", "Ethiopia" },
                { "om", "Itoophiyaa" }
            }},
            { "FI", new Dictionary<string, string>() {
                { "en", "Finland" },
                { "fi", "Suomi" },
                { "nb", "Finland" },
                { "nl", "Finland" }
            }},
            { "FJ", new Dictionary<string, string>() {
                { "en", "Fiji" }
            }},
            { "FK", new Dictionary<string, string>() {
                { "en", "Falkland Islands" }
            }},
            { "FM", new Dictionary<string, string>() {
                { "en", "Micronesia" }
            }},
            { "FO", new Dictionary<string, string>() {
                { "da", "Færøerne" },
                { "en", "Faroe Islands" },
                { "fo", "Føroyar" }
            }},
            { "FR", new Dictionary<string, string>() {
                { "en", "France" },
                { "fr", "France" },
                { "nb", "Frankrike" },
                { "nl", "Frankrijk" }
            }},
            { "GA", new Dictionary<string, string>() {
                { "en", "Gabon" },
                { "fr", "Gabon" }
            }},
            { "GB", new Dictionary<string, string>() {
                { "en", "United Kingdom" }
            }},
            { "GD", new Dictionary<string, string>() {
                { "en", "Grenada" }
            }},
            { "GE", new Dictionary<string, string>() {
                { "en", "Georgia" },
                { "ka", "საქართველო" }
            }},
            { "GF", new Dictionary<string, string>() {
                { "en", "French Guiana" },
                { "fr", "Guyane française" }
            }},
            { "GG", new Dictionary<string, string>() {
                { "en", "Guernsey" }
            }},
            { "GH", new Dictionary<string, string>() {
                { "en", "Ghana" }
            }},
            { "GI", new Dictionary<string, string>() {
                { "en", "Gibraltar" }
            }},
            { "GL", new Dictionary<string, string>() {
                { "da", "Grønland" },
                { "en", "Greenland" },
                { "kl", "Kalaallit Nunaat" }
            }},
            { "GM", new Dictionary<string, string>() {
                { "en", "The Gambia" }
            }},
            { "GN", new Dictionary<string, string>() {
                { "en", "Guinea" },
                { "fr", "Guinée" }
            }},
            { "GP", new Dictionary<string, string>() {
                { "en", "Guadeloupe" },
                { "fr", "Guadeloupe" }
            }},
            { "GQ", new Dictionary<string, string>() {
                { "en", "Equatorial Guinea" },
                { "es", "Guiena ecuatorial" },
                { "fr", "Guinée équatoriale" },
                { "pt", "Guiné Equatorial" }
            }},
            { "GR", new Dictionary<string, string>() {
                { "el", "Ελλάδα" },
                { "en", "Greece" }
            }},
            { "GS", new Dictionary<string, string>() {
                { "en", "South Georgia and the South Sandwich Islands" }
            }},
            { "GT", new Dictionary<string, string>() {
                { "en", "Guatemala" },
                { "es", "Guatemala" }
            }},
            { "GU", new Dictionary<string, string>() {
                { "ch", "Guåhån" },
                { "en", "Guam" }
            }},
            { "GW", new Dictionary<string, string>() {
                { "en", "Guinea Bissau" },
                { "pt", "Guiné-Bissau" }
            }},
            { "GY", new Dictionary<string, string>() {
                { "en", "Guyana" }
            }},
            { "HK", new Dictionary<string, string>() {
                { "en", "Hong Kong" },
                { "zh-hant", "香港" }
            }},
            { "HM", new Dictionary<string, string>() {
                { "en", "Heard Island and McDonald Islands" }
            }},
            { "HN", new Dictionary<string, string>() {
                { "en", "Honduras" },
                { "es", "Honduras" }
            }},
            { "HR", new Dictionary<string, string>() {
                { "en", "Croatia" },
                { "hr", "Hrvatska" }
            }},
            { "HT", new Dictionary<string, string>() {
                { "en", "Haiti" },
                { "fr", "Haïti" },
                { "ht", "Ayiti" }
            }},
            { "HU", new Dictionary<string, string>() {
                { "en", "Hungary" },
                { "hu", "Magyarország" }
            }},
            { "ID", new Dictionary<string, string>() {
                { "en", "Indonesia" },
                { "id", "Indonesia" }
            }},
            { "IE", new Dictionary<string, string>() {
                { "en", "Ireland" },
                { "ga", "Éire" }
            }},
            { "IL", new Dictionary<string, string>() {
                { "en", "Israel" },
                { "he", "ישראל" }
            }},
            { "IM", new Dictionary<string, string>() {
                { "en", "Isle of Man" }
            }},
            { "IN", new Dictionary<string, string>() {
                { "en", "India" },
                { "hi", "भारत" }
            }},
            { "IO", new Dictionary<string, string>() {
                { "en", "British Indian Ocean Territory" }
            }},
            { "IQ", new Dictionary<string, string>() {
                { "ar", "العراق" },
                { "en", "Iraq" },
                { "ku", "Iraq" }
            }},
            { "IR", new Dictionary<string, string>() {
                { "en", "Iran" },
                { "fa", "ایران" }
            }},
            { "IS", new Dictionary<string, string>() {
                { "en", "Iceland" },
                { "is", "Ísland" }
            }},
            { "IT", new Dictionary<string, string>() {
                { "en", "Italy" },
                { "it", "Italia" }
            }},
            { "JE", new Dictionary<string, string>() {
                { "en", "Jersey" }
            }},
            { "JM", new Dictionary<string, string>() {
                { "en", "Jamaica" }
            }},
            { "JO", new Dictionary<string, string>() {
                { "ar", "الأُرْدُن" },
                { "en", "Jordan" }
            }},
            { "JP", new Dictionary<string, string>() {
                { "en", "Japan" },
                { "ja", "日本" }
            }},
            { "KE", new Dictionary<string, string>() {
                { "en", "Kenya" },
                { "sw", "Kenya" }
            }},
            { "KG", new Dictionary<string, string>() {
                { "en", "Kyrgyzstan" },
                { "ky", "Кыргызстан" },
                { "ru", "Киргизия" }
            }},
            { "KH", new Dictionary<string, string>() {
                { "en", "Cambodia" },
                { "km", "កម្ពុជា" }
            }},
            { "KI", new Dictionary<string, string>() {
                { "en", "Kiribati" }
            }},
            { "KM", new Dictionary<string, string>() {
                { "ar", "ﺍﻟﻘﻤﺮي" },
                { "en", "Comores" },
                { "fr", "Comores" },
                { "sw", "Komori" }
            }},
            { "KN", new Dictionary<string, string>() {
                { "en", "Saint Kitts and Nevis" }
            }},
            { "KP", new Dictionary<string, string>() {
                { "en", "North Korea" },
                { "ko", "북조선" }
            }},
            { "KR", new Dictionary<string, string>() {
                { "en", "South Korea" },
                { "ko", "대한민국" }
            }},
            { "KW", new Dictionary<string, string>() {
                { "ar", "الكويت" },
                { "en", "Kuwait" }
            }},
            { "KY", new Dictionary<string, string>() {
                { "en", "Cayman Islands" }
            }},
            { "KZ", new Dictionary<string, string>() {
                { "en", "Kazakhstan" },
                { "kk", "Қазақстан" },
                { "ru", "Казахстан" }
            }},
            { "LA", new Dictionary<string, string>() {
                { "en", "Laos" },
                { "lo", "ປະຊາຊົນລາວ" }
            }},
            { "LB", new Dictionary<string, string>() {
                { "ar", "لبنان" },
                { "en", "Lebanon" },
                { "fr", "Liban" }
            }},
            { "LC", new Dictionary<string, string>() {
                { "en", "Saint Lucia" }
            }},
            { "LI", new Dictionary<string, string>() {
                { "de", "Liechtenstein" },
                { "en", "Liechtenstein" }
            }},
            { "LK", new Dictionary<string, string>() {
                { "en", "Sri Lanka" },
                { "nb", "Sri Lanka" },
                { "nl", "Sri Lanka" },
                { "si", "ශ්‍රී ලංකා" },
                { "ta", "இலங்கை" }
            }},
            { "LR", new Dictionary<string, string>() {
                { "en", "Liberia" }
            }},
            { "LS", new Dictionary<string, string>() {
                { "en", "Lesotho" }
            }},
            { "LT", new Dictionary<string, string>() {
                { "en", "Lithuania" },
                { "lt", "Lietuva" }
            }},
            { "LU", new Dictionary<string, string>() {
                { "de", "Luxemburg" },
                { "en", "Luxembourg" },
                { "fr", "Luxembourg" },
                { "lb", "Lëtzebuerg" }
            }},
            { "LV", new Dictionary<string, string>() {
                { "en", "Latvia" },
                { "lv", "Latvija" }
            }},
            { "LY", new Dictionary<string, string>() {
                { "ar", "ليبيا" },
                { "en", "Libya" }
            }},
            { "MA", new Dictionary<string, string>() {
                { "ar", "المغرب" },
                { "en", "Morocco" },
                { "fr", "Maroc" },
                { "nb", "Marokko" },
                { "nl", "Marokko" },
                { "zgh", "ⵍⵎⵖⵔⵉⴱ" }
            }},
            { "MC", new Dictionary<string, string>() {
                { "en", "Monaco" },
                { "fr", "Monaco" }
            }},
            { "MD", new Dictionary<string, string>() {
                { "en", "Moldova" },
                { "ro", "Moldova" },
                { "ru", "Молдавия" }
            }},
            { "ME", new Dictionary<string, string>() {
                { "en", "Montenegro" },
                { "sr", "Црна Гора" },
                { "srp", "Crna Gora" }
            }},
            { "MF", new Dictionary<string, string>() {
                { "en", "Saint Martin (French part)" },
                { "fr", "Saint-Martin" }
            }},
            { "MG", new Dictionary<string, string>() {
                { "en", "Madagascar" },
                { "fr", "Madagascar" },
                { "mg", "Madagasikara" }
            }},
            { "MH", new Dictionary<string, string>() {
                { "en", "Marshall Islands" }
            }},
            { "MK", new Dictionary<string, string>() {
                { "en", "Macedonia (Former Yugoslav Republic of)" },
                { "mk", "Македонија" }
            }},
            { "ML", new Dictionary<string, string>() {
                { "en", "Mali" },
                { "fr", "Mali" }
            }},
            { "MM", new Dictionary<string, string>() {
                { "en", "Myanmar" },
                { "my", "မြန်မာ" }
            }},
            { "MN", new Dictionary<string, string>() {
                { "en", "Mongolia" },
                { "mn", "Монгол Улс" }
            }},
            { "MO", new Dictionary<string, string>() {
                { "en", "Macao (SAR of China)" },
                { "pt", "Macau" },
                { "zh-hant", "澳門" }
            }},
            { "MP", new Dictionary<string, string>() {
                { "en", "Northern Mariana Islands" }
            }},
            { "MQ", new Dictionary<string, string>() {
                { "en", "Martinique" },
                { "fr", "Martinique" }
            }},
            { "MR", new Dictionary<string, string>() {
                { "ar", "موريتانيا" },
                { "en", "Mauritania" },
                { "fr", "Mauritanie" }
            }},
            { "MS", new Dictionary<string, string>() {
                { "en", "Montserrat" }
            }},
            { "MT", new Dictionary<string, string>() {
                { "en", "Malta" },
                { "mt", "Malta" }
            }},
            { "MU", new Dictionary<string, string>() {
                { "en", "Mauritius" },
                { "fr", "Mauritius" },
                { "mfe", "Maurice" }
            }},
            { "MV", new Dictionary<string, string>() {
                { "en", "Maldives" }
            }},
            { "MW", new Dictionary<string, string>() {
                { "en", "Malawi" }
            }},
            { "MX", new Dictionary<string, string>() {
                { "en", "Mexico" },
                { "es", "México" }
            }},
            { "MY", new Dictionary<string, string>() {
                { "en", "Malaysia" }
            }},
            { "MZ", new Dictionary<string, string>() {
                { "en", "Mozambique" },
                { "pt", "Mozambique" }
            }},
            { "NA", new Dictionary<string, string>() {
                { "en", "Namibia" }
            }},
            { "NC", new Dictionary<string, string>() {
                { "en", "New Caledonia" },
                { "fr", "Nouvelle-Calédonie" }
            }},
            { "NE", new Dictionary<string, string>() {
                { "en", "Niger" },
                { "fr", "Niger" }
            }},
            { "NF", new Dictionary<string, string>() {
                { "en", "Norfolk Island" }
            }},
            { "NG", new Dictionary<string, string>() {
                { "en", "Nigeria" }
            }},
            { "NI", new Dictionary<string, string>() {
                { "en", "Nicaragua" },
                { "es", "Nicaragua" }
            }},
            { "NL", new Dictionary<string, string>() {
                { "da", "Holland" },
                { "en", "The Netherlands" },
                { "nb", "Nederland" },
                { "nl", "Nederland" }
            }},
            { "NO", new Dictionary<string, string>() {
                { "da", "Norge" },
                { "en", "Norway" },
                { "nb", "Norge" },
                { "nl", "Noorwegen" },
                { "nn", "Noreg" }
            }},
            { "NP", new Dictionary<string, string>() {
                { "en", "Nepal" }
            }},
            { "NR", new Dictionary<string, string>() {
                { "en", "Nauru" },
                { "na", "Nauru" }
            }},
            { "NU", new Dictionary<string, string>() {
                { "en", "Niue" },
                { "niu", "Niue" }
            }},
            { "NZ", new Dictionary<string, string>() {
                { "en", "New Zealand" },
                { "mi", "New Zealand" }
            }},
            { "OM", new Dictionary<string, string>() {
                { "ar", "سلطنة عُمان" },
                { "en", "Oman" }
            }},
            { "PA", new Dictionary<string, string>() {
                { "en", "Panama" },
                { "es", "Panama" }
            }},
            { "PE", new Dictionary<string, string>() {
                { "en", "Peru" },
                { "es", "Perú" }
            }},
            { "PF", new Dictionary<string, string>() {
                { "en", "French Polynesia" },
                { "fr", "Polynésie française" }
            }},
            { "PG", new Dictionary<string, string>() {
                { "en", "Papua New Guinea" }
            }},
            { "PH", new Dictionary<string, string>() {
                { "en", "Philippines" }
            }},
            { "PK", new Dictionary<string, string>() {
                { "en", "Pakistan" },
                { "nb", "Pakistan" },
                { "nl", "Pakistan" },
                { "ur", "پاکستان" }
            }},
            { "PL", new Dictionary<string, string>() {
                { "en", "Poland" },
                { "pl", "Polska" }
            }},
            { "PM", new Dictionary<string, string>() {
                { "en", "Saint Pierre and Miquelon" },
                { "fr", "Saint-Pierre-et-Miquelon" }
            }},
            { "PN", new Dictionary<string, string>() {
                { "en", "Pitcairn" }
            }},
            { "PR", new Dictionary<string, string>() {
                { "en", "Puerto Rico" },
                { "es", "Puerto Rico" }
            }},
            { "PS", new Dictionary<string, string>() {
                { "ar", "Palestinian Territory" },
                { "en", "Palestinian Territory" }
            }},
            { "PT", new Dictionary<string, string>() {
                { "en", "Portugal" },
                { "pt", "Portugal" }
            }},
            { "PW", new Dictionary<string, string>() {
                { "en", "Palau" }
            }},
            { "PY", new Dictionary<string, string>() {
                { "en", "Paraguay" },
                { "es", "Paraguay" }
            }},
            { "QA", new Dictionary<string, string>() {
                { "ar", "قطر" },
                { "en", "Qatar" }
            }},
            { "RE", new Dictionary<string, string>() {
                { "en", "Reunion" },
                { "fr", "La Réunion" }
            }},
            { "RO", new Dictionary<string, string>() {
                { "en", "Romania" },
                { "ro", "România" }
            }},
            { "RS", new Dictionary<string, string>() {
                { "en", "Serbia" },
                { "sr", "Србија" }
            }},
            { "RU", new Dictionary<string, string>() {
                { "en", "Russia" },
                { "ru", "Россия" }
            }},
            { "RW", new Dictionary<string, string>() {
                { "en", "Rwanda" },
                { "rw", "Rwanda" }
            }},
            { "SA", new Dictionary<string, string>() {
                { "ar", "السعودية" },
                { "en", "Saudi Arabia" }
            }},
            { "SB", new Dictionary<string, string>() {
                { "en", "Solomon Islands" }
            }},
            { "SC", new Dictionary<string, string>() {
                { "en", "Seychelles" },
                { "fr", "Seychelles" }
            }},
            { "SD", new Dictionary<string, string>() {
                { "ar", "السودان" },
                { "en", "Sudan" }
            }},
            { "SE", new Dictionary<string, string>() {
                { "en", "Sweden" },
                { "sv", "Sverige" }
            }},
            { "SG", new Dictionary<string, string>() {
                { "en", "Singapore" },
                { "zh-hans", "Singapore" }
            }},
            { "SH", new Dictionary<string, string>() {
                { "en", "Saint Helena" }
            }},
            { "SI", new Dictionary<string, string>() {
                { "en", "Slovenia" },
                { "sl", "Slovenija" }
            }},
            { "SJ", new Dictionary<string, string>() {
                { "en", "Svalbard and Jan Mayen" },
                { "no", "Svalbard and Jan Mayen" }
            }},
            { "SK", new Dictionary<string, string>() {
                { "en", "Slovakia" },
                { "sk", "Slovensko" }
            }},
            { "SL", new Dictionary<string, string>() {
                { "en", "Sierra Leone" }
            }},
            { "SM", new Dictionary<string, string>() {
                { "en", "San Marino" },
                { "it", "San Marino" }
            }},
            { "SN", new Dictionary<string, string>() {
                { "en", "Sénégal" },
                { "fr", "Sénégal" }
            }},
            { "SO", new Dictionary<string, string>() {
                { "ar", "الصومال" },
                { "en", "Somalia" },
                { "so", "Somalia" }
            }},
            { "SR", new Dictionary<string, string>() {
                { "en", "Suriname" },
                { "nl", "Suriname" }
            }},
            { "SS", new Dictionary<string, string>() {
                { "en", "South Sudan" }
            }},
            { "ST", new Dictionary<string, string>() {
                { "en", "São Tomé and Príncipe" },
                { "pt", "São Tomé e Príncipe" }
            }},
            { "SV", new Dictionary<string, string>() {
                { "en", "El Salvador" },
                { "es", "El Salvador" }
            }},
            { "SX", new Dictionary<string, string>() {
                { "en", "Saint Martin (Dutch part)" },
                { "nl", "Sint Maarten" }
            }},
            { "SY", new Dictionary<string, string>() {
                { "ar", "سوريا" },
                { "en", "Syria" }
            }},
            { "SZ", new Dictionary<string, string>() {
                { "en", "Swaziland" }
            }},
            { "TC", new Dictionary<string, string>() {
                { "en", "Turks and Caicos Islands" }
            }},
            { "TD", new Dictionary<string, string>() {
                { "ar", "تشاد" },
                { "en", "Chad" },
                { "fr", "Tchad" }
            }},
            { "TF", new Dictionary<string, string>() {
                { "en", "French Southern and Antarctic Lands" },
                { "fr", "Terres australes et antarctiques françaises" }
            }},
            { "TG", new Dictionary<string, string>() {
                { "en", "Togo" },
                { "fr", "Togo" }
            }},
            { "TH", new Dictionary<string, string>() {
                { "en", "Thailand" },
                { "th", "ประเทศไทย" }
            }},
            { "TJ", new Dictionary<string, string>() {
                { "en", "Tajikistan" }
            }},
            { "TK", new Dictionary<string, string>() {
                { "en", "Tokelau" },
                { "tkl", "Tokelau" }
            }},
            { "TL", new Dictionary<string, string>() {
                { "en", "Timor-Leste" },
                { "pt", "Timor-Leste" },
                { "tet", "Timor Lorosa'e" }
            }},
            { "TM", new Dictionary<string, string>() {
                { "en", "Turkmenistan" },
                { "tk", "Türkmenistan" }
            }},
            { "TN", new Dictionary<string, string>() {
                { "ar", "تونس" },
                { "en", "Tunisia" },
                { "fr", "Tunisie" }
            }},
            { "TO", new Dictionary<string, string>() {
                { "en", "Tonga" }
            }},
            { "TR", new Dictionary<string, string>() {
                { "en", "Turkey" },
                { "tr", "Türkiye" }
            }},
            { "TT", new Dictionary<string, string>() {
                { "en", "Trinidad and Tobago" }
            }},
            { "TV", new Dictionary<string, string>() {
                { "en", "Tuvalu" }
            }},
            { "TW", new Dictionary<string, string>() {
                { "en", "Taiwan" },
                { "zh-hant", "Taiwan" }
            }},
            { "TZ", new Dictionary<string, string>() {
                { "en", "Tanzania" },
                { "sw", "Tanzania" }
            }},
            { "UA", new Dictionary<string, string>() {
                { "en", "Ukraine" },
                { "nb", "Ukraina" },
                { "nl", "Oekraïne" },
                { "uk", "Україна" }
            }},
            { "UG", new Dictionary<string, string>() {
                { "en", "Uganda" },
                { "nb", "Uganda" },
                { "nl", "Oeganda" }
            }},
            { "UM", new Dictionary<string, string>() {
                { "en", "United States Minor Outlying Islands" }
            }},
            { "US", new Dictionary<string, string>() {
                { "en", "United States of America" }
            }},
            { "UY", new Dictionary<string, string>() {
                { "en", "Uruguay" },
                { "es", "Uruguay" }
            }},
            { "UZ", new Dictionary<string, string>() {
                { "en", "Uzbekistan" }
            }},
            { "VA", new Dictionary<string, string>() {
                { "en", "City of the Vatican" },
                { "it", "Città del Vaticano" }
            }},
            { "VC", new Dictionary<string, string>() {
                { "en", "Saint Vincent and the Grenadines" }
            }},
            { "VE", new Dictionary<string, string>() {
                { "en", "Venezuela" },
                { "es", "Venezuela" }
            }},
            { "VG", new Dictionary<string, string>() {
                { "en", "British Virgin Islands" }
            }},
            { "VI", new Dictionary<string, string>() {
                { "en", "United States Virgin Islands" }
            }},
            { "VN", new Dictionary<string, string>() {
                { "en", "Vietnam" },
                { "vi", "Việt Nam" }
            }},
            { "VU", new Dictionary<string, string>() {
                { "bi", "Vanuatu" },
                { "en", "Vanuatu" }
            }},
            { "WF", new Dictionary<string, string>() {
                { "en", "Wallis and Futuna" },
                { "fr", "Wallis-et-Futuna" }
            }},
            { "WS", new Dictionary<string, string>() {
                { "en", "Samoa" },
                { "sm", "Samoa" }
            }},
            { "YE", new Dictionary<string, string>() {
                { "ar", "اليَمَن" },
                { "en", "Yemen" }
            }},
            { "YT", new Dictionary<string, string>() {
                { "en", "Mayotte" },
                { "fr", "Mayotte" }
            }},
            { "ZA", new Dictionary<string, string>() {
                { "en", "South Africa" }
            }},
            { "ZM", new Dictionary<string, string>() {
                { "en", "Zambia" }
            }},
            { "ZW", new Dictionary<string, string>() {
                { "en", "Zimbabwe" }
            } }
        };

        #endregion

        #region Properties

        /// <summary>
        /// Two-letter ISO 3166 country code
        /// </summary>
        public string Code { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a country
        /// </summary>
        /// <param name="code">Two-letter ISO 3166 country code</param>
        public Country(string code)
        {
            Code = code;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            // Some countries (e.g. AQ - Antarctica) are not defined in .NET.
            try
            {
                return new RegionInfo(Code).DisplayName;
            }
            catch (ArgumentException)
            {
                return Countries.TryGetValue(Code, out var dict) ? dict.GetLocalized(Code) : Code;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Country;
            if (!Code.Equals(other.Code))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            return ToString().CompareTo(obj.ToString());
        }

        public static bool operator ==(Country left, Country right)
        {
            if (left is null)
                return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Country left, Country right)
        {
            return !(left == right);
        }

        public static bool operator <(Country left, Country right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Country left, Country right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Country left, Country right)
        {
            return left is object && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Country left, Country right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion
    }
}
