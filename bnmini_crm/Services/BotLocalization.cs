namespace bnmini_crm.Services;

public static class BotLocalization
{
    public static BotTexts Get(string language) => language switch
    {
        "ru" => new BotTexts
        {
            Welcome = "👋 Добро пожаловать в *{0}*!",
            OperatorWelcome = "👋 Привет, оператор *{0}*!\n\nЗдесь будут приходить заказы. Отвечайте *'ок'* на сообщение заказа для подтверждения.",
            BossWelcome = "👋 *{0}* — Панель Boss\n\nВыберите отчёт:",
            OrderAccepted = "✅ *Ваш заказ #{0} принят!*\n\n{1}\n\n🚚 Доставка — {2}\n💰 Итого — *{3}*\n\n📍 Адрес: {4}\n⏰ Время: {5}\n📞 Телефон: {6}",
            OrderConfirmed = "✅ *Ваш заказ #{0} подтверждён!*\nМы начали его готовить 👨‍🍳",
            OrderCancelled = "❌ *Ваш заказ #{0} отменён.*\nПричина: {1}",
            NewOrder = "🆕 *Новый заказ #{0}*\n\n{1}\n\n💰 Итого — *{2}*\n📍 Адрес: {3}\n⏰ Время: {4}\n📞 Телефон: {5}\n👤 Клиент: {6}\n\n_Ответьте 'ок' на это сообщение чтобы подтвердить_",
            OrderConfirmedOperator = "✅ Заказ #{0} подтверждён. Клиент уведомлён.",
            About = "ℹ️ О нас",
            MakeOrder = "🛒 Сделать заказ",
            ConfirmWords = new[] { "ок", "ok", "окей", "подтверждаю", "принято", "👍" },
            Delivery = "Доставка",
            RevenueReport = "💰 *Прибыль — {0}*\n\n📅 Сегодня: *{1}* ({2} заказов)\n📊 Всего: *{3}* ({4} заказов)\n🧾 Средний чек: *{5}*",
            ItemsReport = "📦 *По товарам — {0}*\n_(только подтверждённые)_\n\n{1}\n\n💰 Итого: *{2}*",
            CatalogReport = "🗂 *Каталог — {0}*\n\n🛍 Товаров: *{1}*\n📂 Категорий: *{2}*",
            ClientsReport = "👥 *Клиенты — {0}*\n\n🆕 Новых сегодня: *{1}*\n📊 Всего клиентов: *{2}*",
            OrdersReport = "📋 *Заказы — {0}*\n\n*Сегодня:*\n• Всего: {1}\n• ✅ Подтверждено: {2}\n• 🆕 Новых: {3}\n\n*За всё время:*\n• Всего: {4}\n• ✅ Подтверждено: {5}\n• 🆕 Новых: {6}",
            NoOrdersYet = "📦 Подтверждённых заказов пока нет.",
            BtnRevenue = "💰 Прибыль",
            BtnItemsRevenue = "📦 По товарам",
            BtnCatalog = "🗂 Каталог",
            BtnClients = "👥 Клиенты",
            BtnOrders = "📋 Заказы",
            BtnPanel = "🖥 Панель управления",
            BtnAbout = "ℹ️ О нас",
            PressStart = "Нажмите /start чтобы открыть меню.",
            Lang = "ru",
        },
        "tk" => new BotTexts
        {
            Welcome = "👋 *{0}* hoş geldiňiz!",
            OperatorWelcome = "👋 Salam, operator *{0}*!\n\nBu ýere sargytlar geler. Sargydy tassyklamak üçin sargyt habarna *'ok'* diýip jogap beriň.",
            BossWelcome = "👋 *{0}* — Boss paneli\n\nHesabaty saýlaň:",
            OrderAccepted = "✅ *Sargydyňyz #{0} kabul edildi!*\n\n{1}\n\n🚚 Eltip beriş — {2}\n💰 Jemi — *{3}*\n\n📍 Salgy: {4}\n⏰ Wagt: {5}\n📞 Telefon: {6}",
            OrderConfirmed = "✅ *Sargydyňyz #{0} tassyklandy!*\nBiz taýýarlamaga başladyk 👨‍🍳",
            OrderCancelled = "❌ *Sargydyňyz #{0} ýatyryldy.*\nSebäbi: {1}",
            NewOrder = "🆕 *Täze sargyt #{0}*\n\n{1}\n\n💰 Jemi — *{2}*\n📍 Salgy: {3}\n⏰ Wagt: {4}\n📞 Telefon: {5}\n👤 Müşderi: {6}\n\n_Tassyklamak üçin bu habara 'ok' diýip jogap beriň_",
            OrderConfirmedOperator = "✅ Sargyt #{0} tassyklandy. Müşderi habardar edildi.",
            About = "ℹ️ Biz hakda",
            MakeOrder = "🛒 Sargyt etmek",
            ConfirmWords = new[] { "ok", "ок", "tassyk", "bolýar", "👍" },
            Delivery = "Eltip beriş",
            RevenueReport = "💰 *Girdeji — {0}*\n\n📅 Şu gün: *{1}* ({2} sargyt)\n📊 Jemi: *{3}* ({4} sargyt)\n🧾 Ortaça: *{5}*",
            ItemsReport = "📦 *Harytlar boýunça — {0}*\n_(diňe tassyklananlar)_\n\n{1}\n\n💰 Jemi: *{2}*",
            CatalogReport = "🗂 *Katalog — {0}*\n\n🛍 Harytlar: *{1}*\n📂 Kategoriýalar: *{2}*",
            ClientsReport = "👥 *Müşderiler — {0}*\n\n🆕 Şu gün täze: *{1}*\n📊 Jemi müşderi: *{2}*",
            OrdersReport = "📋 *Sargytlar — {0}*\n\n*Şu gün:*\n• Jemi: {1}\n• ✅ Tassyklanan: {2}\n• 🆕 Täze: {3}\n\n*Ählisi:*\n• Jemi: {4}\n• ✅ Tassyklanan: {5}\n• 🆕 Täze: {6}",
            NoOrdersYet = "📦 Tassyklanan sargyt ýok.",
            BtnRevenue = "💰 Girdeji",
            BtnItemsRevenue = "📦 Harytlar boýunça",
            BtnCatalog = "🗂 Katalog",
            BtnClients = "👥 Müşderiler",
            BtnOrders = "📋 Sargytlar",
            BtnPanel = "🖥 Dolandyryş paneli",
            BtnAbout = "ℹ️ Biz hakda",
            PressStart = "/start basyň.",
            Lang = "tk",
        },
        _ => new BotTexts
        {
            Welcome = "👋 Welcome to *{0}*!",
            OperatorWelcome = "👋 Hello, operator *{0}*!\n\nOrders will come here. Reply *'ok'* to confirm an order.",
            BossWelcome = "👋 *{0}* — Boss Panel\n\nSelect a report:",
            OrderAccepted = "✅ *Your order #{0} accepted!*\n\n{1}\n\n🚚 Delivery — {2}\n💰 Total — *{3}*\n\n📍 Address: {4}\n⏰ Time: {5}\n📞 Phone: {6}",
            OrderConfirmed = "✅ *Your order #{0} confirmed!*\nWe started cooking 👨‍🍳",
            OrderCancelled = "❌ *Your order #{0} cancelled.*\nReason: {1}",
            NewOrder = "🆕 *New order #{0}*\n\n{1}\n\n💰 Total — *{2}*\n📍 Address: {3}\n⏰ Time: {4}\n📞 Phone: {5}\n👤 Client: {6}\n\n_Reply 'ok' to confirm_",
            OrderConfirmedOperator = "✅ Order #{0} confirmed. Client notified.",
            About = "ℹ️ About us",
            MakeOrder = "🛒 Place Order",
            ConfirmWords = new[] { "ok", "ок", "yes", "confirm", "👍" },
            Delivery = "Delivery",
            RevenueReport = "💰 *Revenue — {0}*\n\n📅 Today: *{1}* ({2} orders)\n📊 Total: *{3}* ({4} orders)\n🧾 Avg check: *{5}*",
            ItemsReport = "📦 *By items — {0}*\n_(confirmed only)_\n\n{1}\n\n💰 Total: *{2}*",
            CatalogReport = "🗂 *Catalog — {0}*\n\n🛍 Items: *{1}*\n📂 Categories: *{2}*",
            ClientsReport = "👥 *Clients — {0}*\n\n🆕 New today: *{1}*\n📊 Total clients: *{2}*",
            OrdersReport = "📋 *Orders — {0}*\n\n*Today:*\n• Total: {1}\n• ✅ Confirmed: {2}\n• 🆕 New: {3}\n\n*All time:*\n• Total: {4}\n• ✅ Confirmed: {5}\n• 🆕 New: {6}",
            NoOrdersYet = "📦 No confirmed orders yet.",
            BtnRevenue = "💰 Revenue",
            BtnItemsRevenue = "📦 By items",
            BtnCatalog = "🗂 Catalog",
            BtnClients = "👥 Clients",
            BtnOrders = "📋 Orders",
            BtnPanel = "🖥 Manager Panel",
            BtnAbout = "ℹ️ About us",
            PressStart = "Press /start to open menu.",
            Lang = "en",
        }
    };
}

public class BotTexts
{
    public string Welcome { get; set; } = "";
    public string OperatorWelcome { get; set; } = "";
    public string BossWelcome { get; set; } = "";
    public string OrderAccepted { get; set; } = "";
    public string OrderConfirmed { get; set; } = "";
    public string OrderCancelled { get; set; } = "";
    public string NewOrder { get; set; } = "";
    public string OrderConfirmedOperator { get; set; } = "";
    public string About { get; set; } = "";
    public string MakeOrder { get; set; } = "";
    public string[] ConfirmWords { get; set; } = new[] { "ok" };
    public string Delivery { get; set; } = "";
    public string RevenueReport { get; set; } = "";
    public string ItemsReport { get; set; } = "";
    public string CatalogReport { get; set; } = "";
    public string ClientsReport { get; set; } = "";
    public string OrdersReport { get; set; } = "";
    public string NoOrdersYet { get; set; } = "";
    public string BtnRevenue { get; set; } = "";
    public string BtnItemsRevenue { get; set; } = "";
    public string BtnCatalog { get; set; } = "";
    public string BtnClients { get; set; } = "";
    public string BtnOrders { get; set; } = "";
    public string BtnPanel { get; set; } = "";
    public string BtnAbout { get; set; } = "";
    public string PressStart { get; set; } = "";
    public string Lang { get; set; } = "en";
}