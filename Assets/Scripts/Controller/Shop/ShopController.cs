using SnowPlow.Controller.Spawning;
using SnowPlow.Model.Players;
using SnowPlow.Model.Shop;
using SnowPlow.Model.Tools;
using SnowPlowVehicle = SnowPlow.Model.Vehicles.SnowPlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SnowPlow.Controller.Shop
{
    public class ShopController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VehicleSpawner vehicleSpawner;

        [Header("Shop Root")]
        [SerializeField] private GameObject shopRoot;

        [Header("Texts")]
        [SerializeField] private TMP_Text moneyText;

        [Header("NPC Price Texts")]
        [SerializeField] private TMP_Text npcSweaperPriceText;
        [SerializeField] private TMP_Text npcIceBreakerPriceText;

        [Header("Fuel Bars")]
        [SerializeField] private Slider dragonFuelBar;
        [SerializeField] private Slider saltFuelBar;

        [SerializeField] private int dragonFuelDisplayMax = 100;
        [SerializeField] private int saltFuelDisplayMax = 100;

        [SerializeField] private TMP_Text sweaperStatusText;
        [SerializeField] private TMP_Text iceBreakerStatusText;
        [SerializeField] private TMP_Text vomitStatusText;
        [SerializeField] private TMP_Text saltStatusText;
        [SerializeField] private TMP_Text dragonStatusText;

        [SerializeField] private TMP_Text dragonFuelText;
        [SerializeField] private TMP_Text saltFuelText;

        [Header("NPC Buttons")]
        [SerializeField] private Button buyNpcSweaperButton;
        [SerializeField] private Button buyNpcIceBreakerButton;

        [Header("Tool Buttons")]
        [SerializeField] private Button buySweaperButton;
        [SerializeField] private Button equipSweaperButton;

        [SerializeField] private Button buyIceBreakerButton;
        [SerializeField] private Button equipIceBreakerButton;

        [SerializeField] private Button buyVomitButton;
        [SerializeField] private Button equipVomitButton;

        [SerializeField] private Button buySaltButton;
        [SerializeField] private Button equipSaltButton;

        [SerializeField] private Button buyDragonButton;
        [SerializeField] private Button equipDragonButton;

        [Header("Fuel Buttons")]
        [SerializeField] private Button buyDragonFuelButton;
        [SerializeField] private Button buySaltFuelButton;

        private bool isVisibleForSnowPlowPlayer;

        private void Start()
        {
            StartCoroutine(RefreshAfterSceneSetup());
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private IEnumerator RefreshAfterSceneSetup()
        {
            yield return null;
            RefreshUI();

            yield return null;
            RefreshUI();
        }

        public void SetVisibleForSnowPlowPlayer(bool visible)
        {
            isVisibleForSnowPlowPlayer = visible;

            if (shopRoot != null)
            {
                shopRoot.SetActive(visible);
            }

            RefreshUI();
            StartCoroutine(RefreshAfterSceneSetup());

            Debug.Log("Shop visibility for snowplow player: " + visible);
        }

        public void BuyNpcSweaperSnowPlow()
        {
            BuyNpcSnowPlow(ShopCatalog.NpcSweaperSnowPlowPrice, new SweaperTool());
            RefreshUI();
        }

        public void BuyNpcIceBreakerSnowPlow()
        {
            BuyNpcSnowPlow(ShopCatalog.NpcIceBreakerSnowPlowPrice, new IceBreaker());
            RefreshUI();
        }

        public void BuySweaperTool()
        {
            BuyPlayerTool(PlowToolType.Sweaper, ShopCatalog.SweaperToolPrice, new SweaperTool());
            RefreshUI();
        }

        public void BuyIceBreaker()
        {
            BuyPlayerTool(PlowToolType.IceBreaker, ShopCatalog.IceBreakerToolPrice, new IceBreaker());
            RefreshUI();
        }

        public void BuyVomitTool()
        {
            BuyPlayerTool(PlowToolType.Vomit, ShopCatalog.VomitToolPrice, new VomitTool());
            RefreshUI();
        }

        public void BuySaltTool()
        {
            BuyPlayerTool(PlowToolType.Salt, ShopCatalog.SaltToolPrice, new SaltTool());
            RefreshUI();
        }

        public void BuyDragonTool()
        {
            BuyPlayerTool(PlowToolType.Dragon, ShopCatalog.DragonToolPrice, new DragonTool());
            RefreshUI();
        }

        public void EquipSweaperTool()
        {
            EquipPlayerTool(PlowToolType.Sweaper);
            RefreshUI();
        }

        public void EquipIceBreaker()
        {
            EquipPlayerTool(PlowToolType.IceBreaker);
            RefreshUI();
        }

        public void EquipVomitTool()
        {
            EquipPlayerTool(PlowToolType.Vomit);
            RefreshUI();
        }

        public void EquipSaltTool()
        {
            EquipPlayerTool(PlowToolType.Salt);
            RefreshUI();
        }

        public void EquipDragonTool()
        {
            EquipPlayerTool(PlowToolType.Dragon);
            RefreshUI();
        }

        public void BuyDragonFuel()
        {
            Player player = GetCurrentPlayer();
            if (player == null)
            {
                RefreshUI();
                return;
            }

            IPlowTool tool = player.FindOwnedTool(PlowToolType.Dragon);
            if (tool is not DragonTool dragonTool)
            {
                Debug.LogWarning("Cannot buy Dragon fuel: player does not own DragonTool.");
                RefreshUI();
                return;
            }

            if (!TrySpendMoney(ShopCatalog.DragonFuelPrice))
            {
                RefreshUI();
                return;
            }

            dragonTool.AddFuel(ShopCatalog.DragonFuelAmountPerPurchase);

            Debug.Log("Bought Dragon fuel.");

            RefreshUI();
        }

        public void BuySaltFuel()
        {
            Player player = GetCurrentPlayer();
            if (player == null)
            {
                RefreshUI();
                return;
            }

            IPlowTool tool = player.FindOwnedTool(PlowToolType.Salt);
            if (tool is not SaltTool saltTool)
            {
                Debug.LogWarning("Cannot buy Salt fuel: player does not own SaltTool.");
                RefreshUI();
                return;
            }

            if (!TrySpendMoney(ShopCatalog.SaltFuelPrice))
            {
                RefreshUI();
                return;
            }

            saltTool.AddFuel(ShopCatalog.SaltFuelAmountPerPurchase);

            Debug.Log("Bought Salt fuel.");

            RefreshUI();
        }

        public void RefreshUI()
        {
            Player player = GetCurrentPlayerSilently();
            Team team = player?.Team;

            bool hasTeam = team != null;

            if (moneyText != null)
            {
                moneyText.text = hasTeam ? $"{team.Money}$" : "0$";
            }

            RefreshNpcUI();

            SnowPlowVehicle snowPlow = player?.GetOwnedSnowPlow();
            IPlowTool equippedTool = snowPlow?.EquippedTool;

            RefreshToolRow(
                PlowToolType.Sweaper,
                ShopCatalog.SweaperToolPrice,
                sweaperStatusText,
                buySweaperButton,
                equipSweaperButton,
                player,
                equippedTool
            );

            RefreshToolRow(
                PlowToolType.IceBreaker,
                ShopCatalog.IceBreakerToolPrice,
                iceBreakerStatusText,
                buyIceBreakerButton,
                equipIceBreakerButton,
                player,
                equippedTool
            );

            RefreshToolRow(
                PlowToolType.Vomit,
                ShopCatalog.VomitToolPrice,
                vomitStatusText,
                buyVomitButton,
                equipVomitButton,
                player,
                equippedTool
            );

            RefreshToolRow(
                PlowToolType.Salt,
                ShopCatalog.SaltToolPrice,
                saltStatusText,
                buySaltButton,
                equipSaltButton,
                player,
                equippedTool
            );

            RefreshToolRow(
                PlowToolType.Dragon,
                ShopCatalog.DragonToolPrice,
                dragonStatusText,
                buyDragonButton,
                equipDragonButton,
                player,
                equippedTool
            );

            RefreshFuelUI(player, hasTeam);

            bool canBuyNpcSweaper =
                isVisibleForSnowPlowPlayer &&
                hasTeam &&
                team.CanAfford(ShopCatalog.NpcSweaperSnowPlowPrice);

            bool canBuyNpcIceBreaker =
                isVisibleForSnowPlowPlayer &&
                hasTeam &&
                team.CanAfford(ShopCatalog.NpcIceBreakerSnowPlowPrice);

            if (buyNpcSweaperButton != null)
            {
                buyNpcSweaperButton.interactable = canBuyNpcSweaper;
            }

            if (buyNpcIceBreakerButton != null)
            {
                buyNpcIceBreakerButton.interactable = canBuyNpcIceBreaker;
            }
        }

        private void BuyNpcSnowPlow(int price, IPlowTool tool)
        {
            if (!CanUseShop()) return;

            if (vehicleSpawner == null)
            {
                Debug.LogWarning("Cannot buy NPC SnowPlow: VehicleSpawner is missing.");
                return;
            }

            if (!TrySpendMoney(price)) return;

            vehicleSpawner.SpawnSnowPlowNPC(tool);

            Debug.Log("Bought NPC SnowPlow with tool: " + tool.Type());
        }

        private void BuyPlayerTool(PlowToolType type, int price, IPlowTool tool)
        {
            if (!CanUseShop()) return;

            Player player = GetCurrentPlayer();
            if (player == null) return;

            if (player.HasTool(type))
            {
                Debug.LogWarning("Cannot buy tool: already owned: " + type);
                return;
            }

            if (!TrySpendMoney(price)) return;

            player.AddPlowTool(tool);

            Debug.Log("Bought player tool: " + type);
        }

        private void EquipPlayerTool(PlowToolType type)
        {
            if (!CanUseShop()) return;

            Player player = GetCurrentPlayer();
            if (player == null) return;

            SnowPlowVehicle snowPlow = player.GetOwnedSnowPlow();
            if (snowPlow == null)
            {
                Debug.LogWarning("Cannot equip tool: player has no SnowPlow.");
                return;
            }

            IPlowTool ownedTool = player.FindOwnedTool(type);
            if (ownedTool == null)
            {
                Debug.LogWarning("Cannot equip tool: player does not own tool: " + type);
                return;
            }

            snowPlow.EquippedTool = ownedTool;

            Debug.Log("Equipped tool: " + type);
        }

        private void RefreshToolRow(
    PlowToolType type,
    int price,
    TMP_Text statusText,
    Button buyButton,
    Button equipButton,
    Player player,
    IPlowTool equippedTool)
        {
            bool hasPlayer = player != null;
            bool hasTeam = player?.Team != null;
            bool ownsTool = hasPlayer && player.HasTool(type);

            bool isEquipped =
                ownsTool &&
                equippedTool != null &&
                equippedTool.Type() == type;

            if (statusText != null)
            {
                if (isEquipped)
                {
                    statusText.text = "Equipped";
                }
                else if (ownsTool)
                {
                    statusText.text = "Owned";
                }
                else
                {
                    statusText.text = $"{price}$";
                }
            }

            // $ gomb = vásárlás
            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(!ownsTool);

                buyButton.interactable =
                    isVisibleForSnowPlowPlayer &&
                    hasTeam &&
                    !ownsTool &&
                    player.Team.CanAfford(price);
            }

            // + gomb = equip
            // Ne látszódjon, amíg nincs meg a fej.
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(ownsTool);

                equipButton.interactable =
                    isVisibleForSnowPlowPlayer &&
                    ownsTool &&
                    !isEquipped &&
                    player.GetOwnedSnowPlow() != null;
            }
        }

        private void RefreshFuelUI(Player player, bool hasTeam)
        {
            DragonTool dragonTool = player?.FindOwnedTool(PlowToolType.Dragon) as DragonTool;
            SaltTool saltTool = player?.FindOwnedTool(PlowToolType.Salt) as SaltTool;

            if (dragonFuelText != null)
            {
                dragonFuelText.text = dragonTool != null
                    ? $"{dragonTool.Fuel}/{dragonFuelDisplayMax} (+{ShopCatalog.DragonFuelAmountPerPurchase} = {ShopCatalog.DragonFuelPrice}$)"
                    : $"- (+{ShopCatalog.DragonFuelAmountPerPurchase} = {ShopCatalog.DragonFuelPrice}$)";
            }

            if (saltFuelText != null)
            {
                saltFuelText.text = saltTool != null
                    ? $"{saltTool.Fuel}/{saltFuelDisplayMax} (+{ShopCatalog.SaltFuelAmountPerPurchase} = {ShopCatalog.SaltFuelPrice}$)"
                    : $"- (+{ShopCatalog.SaltFuelAmountPerPurchase} = {ShopCatalog.SaltFuelPrice}$)";
            }

            if (dragonFuelBar != null)
            {
                dragonFuelBar.maxValue = dragonFuelDisplayMax;
                dragonFuelBar.value = dragonTool != null
                    ? Mathf.Clamp(dragonTool.Fuel, 0, dragonFuelDisplayMax)
                    : 0;
            }

            if (saltFuelBar != null)
            {
                saltFuelBar.maxValue = saltFuelDisplayMax;
                saltFuelBar.value = saltTool != null
                    ? Mathf.Clamp(saltTool.Fuel, 0, saltFuelDisplayMax)
                    : 0;
            }

            if (buyDragonFuelButton != null)
            {
                bool canBuyDragonFuel =
                    isVisibleForSnowPlowPlayer &&
                    hasTeam &&
                    dragonTool != null &&
                    player.Team.CanAfford(ShopCatalog.DragonFuelPrice);

                buyDragonFuelButton.interactable = canBuyDragonFuel;
                buyDragonFuelButton.gameObject.SetActive(dragonTool != null);
            }

            if (buySaltFuelButton != null)
            {
                bool canBuySaltFuel =
                    isVisibleForSnowPlowPlayer &&
                    hasTeam &&
                    saltTool != null &&
                    player.Team.CanAfford(ShopCatalog.SaltFuelPrice);

                buySaltFuelButton.interactable = canBuySaltFuel;
                buySaltFuelButton.gameObject.SetActive(saltTool != null);
            }
        }

        private bool TrySpendMoney(int amount)
        {
            Team team = GetCurrentTeam();

            if (team == null)
            {
                Debug.LogWarning("Cannot buy item: current player has no team.");
                return false;
            }

            if (!team.TrySpendMoney(amount))
            {
                Debug.LogWarning("Cannot buy item: not enough money.");
                return false;
            }

            return true;
        }

        private bool CanUseShop()
        {
            if (!isVisibleForSnowPlowPlayer)
            {
                Debug.LogWarning("Cannot use shop: player is not a snowplow player.");
                return false;
            }

            return true;
        }

        private Player GetCurrentPlayer()
        {
            if (global::GameManager.Instance == null)
            {
                Debug.LogWarning("Cannot access shop: GameManager is missing.");
                return null;
            }

            if (global::GameManager.Instance.CurrentPlayer == null)
            {
                Debug.LogWarning("Cannot access shop: CurrentPlayer is missing.");
                return null;
            }

            return global::GameManager.Instance.CurrentPlayer;
        }

        private Player GetCurrentPlayerSilently()
        {
            if (global::GameManager.Instance == null) return null;
            return global::GameManager.Instance.CurrentPlayer;
        }

        private Team GetCurrentTeam()
        {
            Player player = GetCurrentPlayer();
            if (player == null) return null;

            return player.Team;
        }

        private void RefreshNpcUI()
        {
            if (npcSweaperPriceText != null)
            {
                npcSweaperPriceText.text = $"{ShopCatalog.NpcSweaperSnowPlowPrice}$";
            }

            if (npcIceBreakerPriceText != null)
            {
                npcIceBreakerPriceText.text = $"{ShopCatalog.NpcIceBreakerSnowPlowPrice}$";
            }
        }
    }
}