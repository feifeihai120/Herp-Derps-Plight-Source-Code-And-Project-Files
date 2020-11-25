﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Spriter2UnityDX;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using TMPro;

public class ShopController : Singleton<ShopController>
{
    // Properties + Components
    #region
    [Header("Properties")]
    private ShopContentResultModel currentShopContentResultData;
    private bool shopIsInteractable = false;

    [Header("Main View Components")]
    [PropertySpace(SpaceBefore = 20, SpaceAfter = 0)]
    [SerializeField] private GameObject mainVisualParent;
    [SerializeField] private CanvasGroup mainCg;

    [Header("Character View References")]
    [PropertySpace(SpaceBefore = 20, SpaceAfter = 0)]
    [SerializeField] private CampSiteCharacterView[] allShopCharacterViews;
    [SerializeField] private GameObject charactersViewParent;

    [Header("Nodes + Location References")]
    [PropertySpace(SpaceBefore = 20, SpaceAfter = 0)]
    [SerializeField] private CampSiteNode[] shopNodes;
    [SerializeField] private Transform offScreenStartPosition;

    [Header("Merchant Properties + Components")]
    [PropertySpace(SpaceBefore = 20, SpaceAfter = 0)]
    [SerializeField] private UniversalCharacterModel merchantUcm;
    [SerializeField] private List<string> merchantBodyParts;
    [SerializeField] private Image[] merchantAccesoryImages;
    [SerializeField] private Color merchantAccessoriesNormalColour;
    [SerializeField] private Color merchantNormalColour;
    [SerializeField] private Color merchantHightlightColour;

    [Header("Shop Card Components")]
    [SerializeField] private ShopCardBox[] allShopCards;

    [Header("Speech Bubble Components")]
    [PropertySpace(SpaceBefore = 20, SpaceAfter = 0)]
    [SerializeField] private CanvasGroup bubbleCg;
    [SerializeField] private TextMeshProUGUI bubbleText;
    [SerializeField] private string[] bubbleGreetings;
    [SerializeField] private string[] bubbleFarewells;
    #endregion

    // Getters
    #region
    public ShopContentResultModel CurrentShopContentResultData
    {
        get { return currentShopContentResultData; }
        private set { currentShopContentResultData = value; }
    }
    public CanvasGroup MainCg
    {
        get { return mainCg; }
        private set { mainCg = value; }
    }
    #endregion

    // Show + Hide Views
    #region
    private void ShowMainPanelView()
    {
        mainCg.DOKill();
        mainCg.alpha = 0;
        mainVisualParent.SetActive(true);      
        mainCg.DOFade(1f, 0.25f);
    }
    private void HideMainPanelView()
    {
        mainCg.DOKill();
        mainVisualParent.SetActive(true);
        Sequence s = DOTween.Sequence();
        s.Append(mainCg.DOFade(0, 0.25f));
        s.OnComplete(() => mainVisualParent.SetActive(false));
    }
    public void EnableCharacterViewParent()
    {
        charactersViewParent.SetActive(true);
    }
    public void DisableCharacterViewParent()
    {
        charactersViewParent.SetActive(false);
    }
    public void ShowShopCardBox(ShopCardBox box)
    {
        box.visualParent.SetActive(true);
    }
    public void HideShopCardBox(ShopCardBox box)
    {
        box.visualParent.SetActive(false);
    }
    #endregion

    // Save + Load Logic
    #region
    public void SaveMyDataToSaveFile(SaveGameData saveFile)
    {
        saveFile.shopData = CurrentShopContentResultData;
    }
    public void BuildMyDataFromSaveFile(SaveGameData saveFile)
    {
        CurrentShopContentResultData = saveFile.shopData;
    }
    
    #endregion

    // Build Views
    #region
    public void BuildAllShopCharacterViews(List<CharacterData> characters)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            BuildShopCharacterView(allShopCharacterViews[i], characters[i]);
        }

        CharacterModelController.BuildModelFromStringReferences(merchantUcm, merchantBodyParts);
    }
    private void BuildShopCharacterView(CampSiteCharacterView view, CharacterData data)
    {
        Debug.LogWarning("CampSiteController.BuildShopCharacterView() called, character data: " + data.myName);

        // Connect data to view
        view.myCharacterData = data;

        // Enable shadow
        view.ucmShadowParent.SetActive(true);

        // Build UCM
        CharacterModelController.BuildModelFromStringReferences(view.characterEntityView.ucm, data.modelParts);
        CharacterModelController.ApplyItemManagerDataToCharacterModelView(data.itemManager, view.characterEntityView.ucm);
    }
    public void BuildAllShopContentFromDataSet(ShopContentResultModel dataSet)
    {
        BuildAllShopCardBoxesFromDataSet(dataSet);
    }
    private void BuildAllShopCardBoxesFromDataSet(ShopContentResultModel dataSet)
    {
        for(int i = 0; i < dataSet.cardsData.Count; i++)
        {
            BuildShopCardBox(allShopCards[i], dataSet.cardsData[i]);
        }
    }
    private void BuildShopCardBox(ShopCardBox box, CardPricePairing data)
    {
        ShowShopCardBox(box);
        box.cppData = data;
        box.goldCostText.text = data.goldCost.ToString();
        CardController.Instance.BuildCardViewModelFromCardData(data.cardData, box.cvm);
    }
    public void SetShopKeeperInteractionState(bool onOrOff)
    {
        shopIsInteractable = onOrOff;
    }
    #endregion

    // Generate cards and content
    #region
    public void SetAndCacheNewShopContentDataSet()
    {
        CurrentShopContentResultData = GenerateNewShopContentDataSet();
    }
    public void ClearShopContentDataSet()
    {
        CurrentShopContentResultData = null;
    }
    public ShopContentResultModel GenerateNewShopContentDataSet()
    {
        ShopContentResultModel scr = new ShopContentResultModel();

        // Get ALL valid cards 
        List<CardData> allValidLootableCards = new List<CardData>();
        List<CardData> validCommons = new List<CardData>();
        List<CardData> validRares = new List<CardData>();
        List<CardData> validEpics = new List<CardData>();

        List<CardData> chosenCards = new List<CardData>();

        // Get all valid lootable cards for every character the player has
        foreach (CharacterData character in CharacterDataController.Instance.AllPlayerCharacters)
        {
            foreach(CardData cd in LootController.Instance.GetAllValidLootableCardsForCharacter(character))
            {
                // Prevent doubling up on cards
                if(allValidLootableCards.Contains(cd) == false)
                {
                    allValidLootableCards.Add(cd);
                }
            }
        }

        // Divide cards by rarity
        foreach(CardData cd in allValidLootableCards)
        {
            if(cd.rarity == Rarity.Common)
            {
                validCommons.Add(cd);
            }
            else if (cd.rarity == Rarity.Rare)
            {
                validRares.Add(cd);
            }
            else if (cd.rarity == Rarity.Epic)
            {
                validEpics.Add(cd);
            }
        }

        // Randomly pick 5 commons, 3 rares and 2 epics
        for(int i = 0; i < 5; i++)
        {
            validCommons.Shuffle();
            chosenCards.Add(validCommons[0]);
            validCommons.Remove(validCommons[0]);
        }
        for (int i = 0; i < 3; i++)
        {
            validRares.Shuffle();
            chosenCards.Add(validRares[0]);
            validRares.Remove(validRares[0]);
        }
        for (int i = 0; i < 2; i++)
        {
            validEpics.Shuffle();
            chosenCards.Add(validEpics[0]);
            validEpics.Remove(validEpics[0]);
        }

        // Create new pairings and randomized prices
        foreach(CardData card in chosenCards)
        {
            scr.cardsData.Add(new CardPricePairing(card));
        }

        return scr;

    }
    #endregion

    // Handle Buy Stuff Logic
    #region
    public void OnShopCardBoxClicked(ShopCardBox box)
    {
        if(PlayerDataManager.Instance.CurrentGold >= box.cppData.goldCost)
        {
            KeyWordLayoutController.Instance.FadeOutMainView();

            // Pay gold price
            PlayerDataManager.Instance.ModifyCurrentGold(-box.cppData.goldCost, true);           

            // Cha ching SFX + coin explosion VFX
            AudioManager.Instance.PlaySoundPooled(Sound.Gold_Cha_Ching);
            VisualEventManager.Instance.CreateVisualEvent(() =>
                VisualEffectManager.Instance.CreateGoldCoinExplosion(box.transform.position, 10000, 2));

            // Sparkle glow trail towards inventory
            LootController.Instance.CreateGreenGlowTrailEffect(box.transform.position, TopBarController.Instance.CharacterRosterButton.transform.position);

            // Hide card box
            HideShopCardBox(box);

            // add card to player card inventory

        }        
    }
    #endregion

    // On Button Click Listeners
    #region
    public void OnMerchantCharacterClicked()
    {
        Debug.LogWarning("OnMerchantCharacterClicked()");
        if (mainVisualParent.activeSelf == false && shopIsInteractable)
        {
            SetMerchantColor(merchantUcm.GetComponent<EntityRenderer>(), merchantNormalColour);
            SetMerchantAccessoriesColor(merchantNormalColour);
            AudioManager.Instance.PlaySound(Sound.GUI_Button_Clicked);
            ShowMainPanelView();
        }
    }
    public void OnMerchantCharacterMouseEnter()
    {
        if (mainVisualParent.activeSelf == false && shopIsInteractable)
        {
            AudioManager.Instance.PlaySoundPooled(Sound.GUI_Button_Mouse_Over);
            SetMerchantColor(merchantUcm.GetComponent<EntityRenderer>(), merchantHightlightColour);
            SetMerchantAccessoriesColor(merchantHightlightColour);
        }
    }
    public void OnMerchantCharacterMouseExit()
    {
        if (mainVisualParent.activeSelf == false && shopIsInteractable)
        {
            SetMerchantColor(merchantUcm.GetComponent<EntityRenderer>(), merchantNormalColour);
            SetMerchantAccessoriesColor(merchantAccessoriesNormalColour);
        }
    }
    public void OnMainPanelBackButtonClicked()
    {
        HideMainPanelView();
    }
    #endregion

    // Colouring
    #region
    public void SetMerchantColor(EntityRenderer view, Color newColor)
    {
        if (view != null)
        {
            view.Color = new Color(newColor.r, newColor.g, newColor.b, view.Color.a);
        }
    }
    public void SetMerchantAccessoriesColor(Color newColor)
    {
        foreach (Image i in merchantAccesoryImages)
        {
            i.DOKill();
            i.DOColor(newColor, 0.2f);
        }
    }
    #endregion

    // Character Movement Events
    #region
    public void MoveAllCharactersToStartPosition()
    {
        for (int i = 0; i < allShopCharacterViews.Length; i++)
        {
            CampSiteCharacterView character = allShopCharacterViews[i];

            // Cancel if data ref is null (happens if player has less then 3 characters
            if (character.myCharacterData == null)
            {
                return;
            }

            character.characterEntityView.ucmMovementParent.transform.position = offScreenStartPosition.position;
        }
    }
    public void MoveAllCharactersToTheirNodes()
    {
        StartCoroutine(MoveAllCharactersToTheirNodesCoroutine());
    }
    private IEnumerator MoveAllCharactersToTheirNodesCoroutine()
    {
        for (int i = 0; i < allShopCharacterViews.Length; i++)
        {
            CampSiteCharacterView character = allShopCharacterViews[i];

            // Normal move to node
            if (character.myCharacterData != null && character.myCharacterData.health > 0)
            {
                // replace this with new move function in future, this function will make characters run through the camp fire
                CharacterEntityController.Instance.MoveEntityToNodeCentre(character.characterEntityView, shopNodes[i].LevelNode, null);
            }

            yield return new WaitForSeconds(0.25f);
        }

    }
    public void MoveCharactersToOffScreenRight()
    {
        StartCoroutine(MoveCharactersToOffScreenRightCoroutine());
    }
    private IEnumerator MoveCharactersToOffScreenRightCoroutine()
    {
        foreach (CampSiteCharacterView character in allShopCharacterViews)
        {
            // Only move existing living characters
            if (character.myCharacterData != null &&
                character.myCharacterData.health > 0)
            {
                CharacterEntityController.Instance.MoveEntityToNodeCentre(character.characterEntityView, LevelManager.Instance.EnemyOffScreenNode, null);
            }
        }

        yield return new WaitForSeconds(3f);
    }
    #endregion

    // Greeting Visual Logic
    #region
    public void DoMerchantGreeting()
    {
        StartCoroutine(DoMerchantGreetingCoroutine());
    }
    private IEnumerator DoMerchantGreetingCoroutine()
    {
        bubbleCg.DOKill();
        bubbleText.text = bubbleGreetings[RandomGenerator.NumberBetween(0, bubbleGreetings.Length - 1)];
        bubbleCg.DOFade(1, 0.5f);
        yield return new WaitForSeconds(3.5f);
        bubbleCg.DOFade(0, 0.5f);
    }
    public void DoMerchantFarewell()
    {
        StartCoroutine(DoMerchantFarewellCoroutine());
    }
    private IEnumerator DoMerchantFarewellCoroutine()
    {
        bubbleCg.DOKill();
        bubbleText.text = bubbleFarewells[RandomGenerator.NumberBetween(0, bubbleFarewells.Length - 1)];
        bubbleCg.DOFade(1, 0.5f);
        yield return new WaitForSeconds(1.75f);
        bubbleCg.DOFade(0, 0.5f);
    }
    #endregion
}