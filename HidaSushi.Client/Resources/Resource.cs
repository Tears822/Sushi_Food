using System.Globalization;
using System.Resources;

namespace HidaSushi.Client.Resources;

/// <summary>
/// A strongly-typed resource class for looking up localized strings, etc.
/// </summary>
public class Resource
{
    private static ResourceManager? _resourceMan;
    private static CultureInfo? _resourceCulture;

    internal Resource()
    {
    }

    /// <summary>
    /// Returns the cached ResourceManager instance used by this class.
    /// </summary>
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static ResourceManager ResourceManager
    {
        get
        {
            if (ReferenceEquals(_resourceMan, null))
            {
                var temp = new ResourceManager("HidaSushi.Client.Resources.Resource", typeof(Resource).Assembly);
                _resourceMan = temp;
            }
            return _resourceMan;
        }
    }

    /// <summary>
    /// Overrides the current thread's CurrentUICulture property for all
    /// resource lookups using this strongly typed resource class.
    /// </summary>
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static CultureInfo? Culture
    {
        get
        {
            return _resourceCulture;
        }
        set
        {
            _resourceCulture = value;
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Home.
    /// </summary>
    public static string Navigation_Home
    {
        get
        {
            return ResourceManager.GetString("Navigation.Home", _resourceCulture) ?? "Navigation.Home";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Menu.
    /// </summary>
    public static string Navigation_Menu
    {
        get
        {
            return ResourceManager.GetString("Navigation.Menu", _resourceCulture) ?? "Navigation.Menu";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Build Roll.
    /// </summary>
    public static string Navigation_BuildRoll
    {
        get
        {
            return ResourceManager.GetString("Navigation.BuildRoll", _resourceCulture) ?? "Navigation.BuildRoll";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Track Order.
    /// </summary>
    public static string Navigation_TrackOrder
    {
        get
        {
            return ResourceManager.GetString("Navigation.TrackOrder", _resourceCulture) ?? "Navigation.TrackOrder";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Login.
    /// </summary>
    public static string Navigation_Login
    {
        get
        {
            return ResourceManager.GetString("Navigation.Login", _resourceCulture) ?? "Navigation.Login";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Cart.
    /// </summary>
    public static string Navigation_Cart
    {
        get
        {
            return ResourceManager.GetString("Navigation.Cart", _resourceCulture) ?? "Navigation.Cart";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Premium Sushi Delivered.
    /// </summary>
    public static string Home_Title
    {
        get
        {
            return ResourceManager.GetString("Home.Title", _resourceCulture) ?? "Home.Title";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Fresh & Delicious.
    /// </summary>
    public static string Home_Subtitle
    {
        get
        {
            return ResourceManager.GetString("Home.Subtitle", _resourceCulture) ?? "Home.Subtitle";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Experience the finest sushi crafted by Chef Jonathan. Fresh ingredients, authentic flavors, delivered to your door..
    /// </summary>
    public static string Home_Description
    {
        get
        {
            return ResourceManager.GetString("Home.Description", _resourceCulture) ?? "Home.Description";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Order Now.
    /// </summary>
    public static string Home_OrderNow
    {
        get
        {
            return ResourceManager.GetString("Home.OrderNow", _resourceCulture) ?? "Home.OrderNow";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Explore Menu.
    /// </summary>
    public static string Home_ExploreMenu
    {
        get
        {
            return ResourceManager.GetString("Home.ExploreMenu", _resourceCulture) ?? "Home.ExploreMenu";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to GodCoin Balance.
    /// </summary>
    public static string GodCoin_Balance
    {
        get
        {
            return ResourceManager.GetString("GodCoin.Balance", _resourceCulture) ?? "GodCoin.Balance";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Exchange Rate.
    /// </summary>
    public static string GodCoin_ExchangeRate
    {
        get
        {
            return ResourceManager.GetString("GodCoin.ExchangeRate", _resourceCulture) ?? "GodCoin.ExchangeRate";
        }
    }

    /// <summary>
    /// Looks up a localized string similar to Save.
    /// </summary>
    public static string Common_Save
    {
        get
        {
            return ResourceManager.GetString("Common.Save", _resourceCulture) ?? "Common.Save";
        }
    }
} 