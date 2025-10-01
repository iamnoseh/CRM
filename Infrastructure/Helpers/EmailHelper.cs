using Domain.DTOs.EmailDTOs;
using Infrastructure.Services.EmailService;
using MimeKit.Text;
using System.Drawing;

namespace Infrastructure.Helpers;

public static class EmailHelper
{
    public static async Task SendLoginDetailsEmailAsync(
        IEmailService emailService,
        string email,
        string username,
        string password,
        string userType,
        string gradientStart = "#5E60CE",
        string gradientEnd = "#4EA8DE")
    {
        var subject = "Маълумоти воридшавӣ ба система";
        var message = GenerateLoginEmailTemplate(username, password, 
            $"Шумо ҳамчун {userType} ба қайд гирифта шудед. Маълумоти воридшавӣ дар поён оварда шудаанд:", 
            gradientStart, gradientEnd, userType);

        await emailService.SendEmail(
            new EmailMessageDto(new[] { email }, subject, message),
            TextFormat.Html);
    }

    public static async Task SendResetPasswordCodeEmailAsync(
        IEmailService emailService,
        string email,
        string code)
    {
        var subject = "Рамзи тасдиқ барои барқарорсозии рамз";
        var message = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(45deg, #FF512F, #DD2476); padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0; text-align: center;'>Барқарорсозии рамз</h1>
                </div>
                <div style='background: #f8f9fa; padding: 20px; border-radius: 0 0 10px 10px;'>
                    <p>Салом!</p>
                    <p>Шумо дархости барқарорсозии рамзро пешниҳод кардед. Рамзи тасдиқ:</p>
                    <div style='background: white; padding: 15px; border-radius: 5px; margin: 15px 0; text-align: center;'>
                        <h2 style='color: #0056b3; letter-spacing: 5px;'>{code}</h2>
                    </div>
                    <p><strong>Диққат:</strong></p>
                    <ul>
                        <li>Рамзи тасдиқ танҳо 10 дақиқа эътибор дорад</li>
                        <li>Агар шумо дархост накарда бошед, ин паёмро нодида гиред</li>
                        <li>Рамзи тасдиқро бо касе мубодила накунед</li>
                    </ul>
                </div>
            </div>";

        await emailService.SendEmail(
            new EmailMessageDto(new[] { email }, subject, message),
            TextFormat.Html);
    }

    public static async Task SendPasswordChangedEmailAsync(
        IEmailService emailService,
        string email)
    {
        var subject = "Тағйири рамзи воридшавӣ";
        var message = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(45deg, #1CB5E0, #000046); padding: 20px; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0; text-align: center;'>Огоҳӣ оид ба бехатарӣ</h1>
                </div>
                <div style='background: #f8f9fa; padding: 20px; border-radius: 0 0 10px 10px;'>
                    <p>Салом!</p>
                    <p>Рамзи воридшавии шумо бо муваффақият иваз карда шуд.</p>
                    <p>Агар шумо ин амалро анҷом надода бошед, лутфан:</p>
                    <ol>
                        <li>Фавран ба система ворид шавед</li>
                        <li>Рамзи худро иваз кунед</li>
                        <li>Бо маъмури система тамос гиред</li>
                    </ol>
                    <p style='color: #dc3545;'><strong>Муҳим:</strong> Бехатарии ҳисоби корбарии худро таъмин кунед.</p>
                </div>
            </div>";

        await emailService.SendEmail(
            new EmailMessageDto(new[] { email }, subject, message),
            TextFormat.Html);
    }

    private static string GenerateLoginEmailTemplate(
        string username, 
        string password, 
        string messageText, 
        string primaryColor = "#5E60CE", 
        string accentColor = "#4EA8DE",
        string userType = "user")
    {
        string primaryLight = AdjustBrightness(primaryColor, 15);
        string primaryDark = AdjustBrightness(primaryColor, -15);
        string accentLight = AdjustBrightness(accentColor, 15);
        
        return $@"
<!DOCTYPE html>
<html lang=""tj"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kavsar Academy - Login</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@400;500;600;700&display=swap');
        
        body {{
            margin: 0;
            padding: 0;
            font-family: 'Montserrat', Arial, sans-serif;
            background-color: #f4f7fa;
        }}
        
        .header {{
            background: linear-gradient(135deg, {primaryColor}, {accentColor}, {primaryLight});
            padding: 30px 0;
            text-align: center;
        }}
        
        .logo {{
            color: white;
            font-size: 28px;
            font-weight: 700;
            margin: 0;
            letter-spacing: 1px;
        }}
        
        .credential-box {{
            border-left: 5px solid {accentColor};
            background-color: #ffffff;
            border-radius: 8px;
            padding: 25px;
            margin: 15px 0;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
        }}
        
        .credential-title {{
            color: {primaryColor};
            font-size: 18px;
            font-weight: 600;
            margin-bottom: 20px;
            border-bottom: 2px solid {accentLight};
            padding-bottom: 10px;
        }}
        
        .credential-label {{
            font-weight: 500;
            color: #4a5568;
            margin-bottom: 8px;
            font-size: 15px;
        }}
        
        .credential-value {{
            font-weight: 600;
            font-size: 16px;
            color: {primaryDark};
            background-color: rgba({HexToRgb(primaryColor)}, 0.1);
            padding: 10px 15px;
            border-radius: 6px;
            border-left: 3px solid {accentColor};
            letter-spacing: 0.5px;
            margin-bottom: 20px;
        }}
        
        .button {{
            background: linear-gradient(to right, {primaryColor}, {accentColor});
            color: white;
            font-weight: 600;
            text-decoration: none;
            padding: 15px 30px;
            border-radius: 50px;
            display: inline-block;
            text-align: center;
            font-size: 16px;
            letter-spacing: 0.5px;
            margin: 20px 0;
            box-shadow: 0 8px 15px rgba({HexToRgb(primaryColor)}, 0.3);
        }}

        /* Адаптивность для мобильных устройств */
        @media only screen and (max-width: 600px) {{
            .container {{
                width: 100% !important;
                padding: 10px !important;
            }}
            
            .header {{
                padding: 20px 0;
            }}
            
            .logo, h1 {{
                font-size: 24px !important;
            }}
            
            .credential-box {{
                padding: 20px;
            }}

            h2 {{
                font-size: 20px !important;
            }}

            p, .credential-label, .credential-value {{
                font-size: 15px !important;
                line-height: 1.6 !important;
            }}

            .button {{
                padding: 14px 28px !important;
                font-size: 15px !important;
            }}
        }}
    </style>
</head>
<body>
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
        <tr>
            <td bgcolor=""#f4f7fa"" align=""center"" style=""padding: 20px 0;"">
                <!-- Контейнер основного содержимого -->
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""600"" class=""container"" style=""background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 5px 25px rgba(0,0,0,0.1);"">
                    <!-- Шапка письма -->
                    <tr>
                        <td class=""header"" bgcolor=""{primaryColor}"" style=""background: linear-gradient(135deg, {primaryColor}, {accentColor}, {primaryLight}); padding: 30px 0; text-align: center;"">
                            <h1 style=""color: white; font-size: 28px; font-weight: 700; margin: 0; letter-spacing: 1px;"">Kavsar Academy</h1>
                            <p style=""color: white; margin: 5px 0 0 0; opacity: 0.9; font-size: 16px;"">{(userType == "Mentor" ? "Платформа для преподавателей" : "Образовательная платформа")}</p>
                        </td>
                    </tr>
                    
                    <!-- Основное содержимое -->
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <!-- Приветствие -->
                            <h2 style=""color: {primaryColor}; font-size: 22px; margin: 0 0 20px 0;"">👋 Салом Алейкум !</h2>
                            
                            <!-- Основное сообщение -->
                            <p style=""color: #4a5568; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">{messageText}</p>
                            
                            <!-- Блок учетных данных -->
                            <div class=""credential-box"" style=""border-left: 5px solid {accentColor}; background-color: #ffffff; border-radius: 8px; padding: 25px; margin: 15px 0; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);"">
                                <h3 class=""credential-title"" style=""color: {primaryColor}; font-size: 18px; font-weight: 600; margin-top: 0; margin-bottom: 20px; border-bottom: 2px solid {accentLight}; padding-bottom: 10px;"">Маълумоти воридшавӣ</h3>
                                
                                <!-- Username -->
                                <p class=""credential-label"" style=""font-weight: 500; color: #4a5568; margin-bottom: 8px; font-size: 15px;"">Username</p>
                                <p class=""credential-value"" style=""font-weight: 600; font-size: 16px; color: {primaryDark}; background-color: rgba({HexToRgb(primaryColor)}, 0.1); padding: 10px 15px; border-radius: 6px; border-left: 3px solid {accentColor}; letter-spacing: 0.5px; margin-bottom: 20px; margin-top: 0;"">{username}</p>
                                
                                <!-- Password -->
                                <p class=""credential-label"" style=""font-weight: 500; color: #4a5568; margin-bottom: 8px; font-size: 15px;"">Password</p>
                                <p class=""credential-value"" style=""font-weight: 600; font-size: 16px; color: {primaryDark}; background-color: rgba({HexToRgb(primaryColor)}, 0.1); padding: 10px 15px; border-radius: 6px; border-left: 3px solid {accentColor}; letter-spacing: 0.5px; margin-bottom: 0; margin-top: 0;"">{password}</p>
                            </div>
                            
                            <!-- Инструкции -->
                            <h3 style=""color: {primaryColor}; font-size: 18px; margin: 30px 0 20px 0;"">Дастурамал:</h3>
                            
                            <!-- Шаг 1 -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""width:100%; margin-bottom: 15px;"">
                                <tbody>
                                    <tr>
                                        <td valign=""top"" style=""width: 43px;"">
                                            <div style=""background-color: {primaryColor}; color: white; width: 28px; height: 28px; border-radius: 50%; text-align: center; line-height: 28px; font-weight: 600;"">1</div>
                                        </td>
                                        <td valign=""top"">
                                            <p style=""margin:0; color: #4a5568; font-size: 15px; line-height: 1.5; padding-top: 2px;"">Бо истифода аз маълумоти боло ба система ворид шавед.</p>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            
                            <!-- Шаг 2 -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""width:100%; margin-bottom: 15px;"">
                                <tbody>
                                    <tr>
                                        <td valign=""top"" style=""width: 43px;"">
                                            <div style=""background-color: {primaryColor}; color: white; width: 28px; height: 28px; border-radius: 50%; text-align: center; line-height: 28px; font-weight: 600;"">2</div>
                                        </td>
                                        <td valign=""top"">
                                            <p style=""margin:0; color: #4a5568; font-size: 15px; line-height: 1.5; padding-top: 2px;"">Пароли худро баъд аз воридшавии аввалин тағйир диҳед.</p>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            
                            <!-- Шаг 3 -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""width:100%; margin-bottom: 15px;"">
                                <tbody>
                                    <tr>
                                        <td valign=""top"" style=""width: 43px;"">
                                            <div style=""background-color: {primaryColor}; color: white; width: 28px; height: 28px; border-radius: 50%; text-align: center; line-height: 28px; font-weight: 600;"">3</div>
                                        </td>
                                        <td valign=""top"">
                                            <p style=""margin:0; color: #4a5568; font-size: 15px; line-height: 1.5; padding-top: 2px;"">Профили худро пурра кунед.</p>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            
                            <!-- Кнопка -->
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""https://crm.kavsaracademy.tj/login"" class=""button"" style=""background: linear-gradient(to right, {primaryColor}, {accentColor}); color: white; font-weight: 600; text-decoration: none; padding: 15px 30px; border-radius: 50px; display: inline-block; text-align: center; font-size: 16px; letter-spacing: 0.5px; box-shadow: 0 8px 15px rgba({HexToRgb(primaryColor)}, 0.3);"">Ворид ба система</a>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Подвал -->
                    <tr>
                        <td style=""background-color: #f8fafc; padding: 25px; text-align: center; border-top: 1px solid #e2e8f0;"">
                            <p style=""color: #718096; font-size: 14px; margin: 0;"">Агар савол дошта бошед, ба почтаи <a href=""mailto:info@kavsaracademy.tj"" style=""color: {primaryColor}; text-decoration: none;"">info@kavsaracademy.tj</a> муроҷиат кунед.</p>
                            <p style=""color: #718096; font-size: 14px; margin: 15px 0 0 0;"">© {DateTime.Now.Year} Кавсар Академия. Ҳамаи ҳуқуқҳо ҳифз шудаанд.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
";
    }

    /// <summary>
    /// Регулирует яркость HEX-цвета
    /// </summary>
    private static string AdjustBrightness(string hexColor, int percent)
    {
        if (string.IsNullOrEmpty(hexColor)) return "#5E60CE";
        if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;
        
        try 
        {
            var color = ColorTranslator.FromHtml(hexColor);
            
            int red = Math.Clamp(color.R + (color.R * percent / 100), 0, 255);
            int green = Math.Clamp(color.G + (color.G * percent / 100), 0, 255);
            int blue = Math.Clamp(color.B + (color.B * percent / 100), 0, 255);
            
            return $"#{red:X2}{green:X2}{blue:X2}";
        }
        catch 
        {
            return "#5E60CE";
        }
    }
    
    private static string HexToRgb(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor)) return "79, 70, 229";
        if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;

        try 
        {
            var color = ColorTranslator.FromHtml(hexColor);
            return $"{color.R}, {color.G}, {color.B}";
        }
        catch 
        {
            return "79, 70, 229";
        }
    }
}