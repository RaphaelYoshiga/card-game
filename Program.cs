using System;
using System.Collections.Generic;
using System.Linq;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    private static PlayerStats _playerStats;
    private static int _turnCount;
    private static PlayerStats _enemyStats;
    private static CardSelector _cardSelector;

    static void Main(string[] args)
    {
        string[] inputs;
        _turnCount = 0;
        _cardSelector = new CardSelector();
        // game loop
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                inputs = Console.ReadLine().Split(' ');

                if (i == 0)
                    _playerStats = new PlayerStats(inputs);
                else
                    _enemyStats = new PlayerStats(inputs);
            }

            int opponentHand = int.Parse(Console.ReadLine());
            int cardCount = int.Parse(Console.ReadLine());

            var cards = new Deck(_playerStats);
            for (int i = 0; i < cardCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');

                cards.Add(new Card(inputs, i));
            }

            var commander = new Commander(cards, _playerStats, _enemyStats, _cardSelector);
            commander.Play(_turnCount);
            _turnCount++;
        }
    }
}

internal class CardSelector
{
    private readonly List<Card> _cards;

    public CardSelector()
    {
        _cards = new List<Card>();
    }

    public void ChoosePick(IEnumerable<Card> cardsChoice)
    {
        cardsChoice = MoreThanFourLateGameCards() ? cardsChoice.Where(p => p.Cost <= 7) : cardsChoice;
        var bestCard = cardsChoice.OrderByDescending(p => p.Score).First();
        Pick(bestCard);
    }

    private bool MoreThanFourLateGameCards()
    {
        var highCostCard = _cards.Count(p => p.Cost > 7);
        return highCostCard > 4;
    }

    private void Pick(Card card)
    {
        _cards.Add(card);
        Console.WriteLine($"PICK {card.Index}");
    }
}


internal class Commander
{
    private readonly SimpleAttackStrategy _simpleAttackStrategy;
    private readonly CardSelector _cardSelector;
    public Commands _commands;
    public Deck Deck { get; }
    public PlayerStats Enemy { get; }
    public PlayerStats PlayerStats { get; }

    public Commander(Deck deck, PlayerStats player, PlayerStats enemy, CardSelector cardSelector)
    {
        PlayerStats = player;
        Deck = deck;
        Enemy = enemy;
        _cardSelector = cardSelector;
        _simpleAttackStrategy = new SimpleAttackStrategy(this);
    }

    public void Play(int turnCount)
    {
        if (turnCount < 30)
            _cardSelector.ChoosePick(Deck);
        else
            PlayTurn(Deck, turnCount);
    }


    private void PlayTurn(Deck deck, int turnCount)
    {
        var adjustTurnCount = turnCount - 30;
        _commands = new Commands();
        var commands = _simpleAttackStrategy.SequentialPlay(deck, adjustTurnCount, _commands);

        Commands.ExecuteCommands(commands);
    }

    public void UseGreenItem(Card target, Card item)
    {
        if (target != null)
        {
            _commands.UseItem(item, target);
            PlayerStats.Mana -= item.Cost;
            target.AddStats(item);
        }
    }

    public void UseRedItem(Card item, Card target)
    {
        UseItemOn(item, target);

        target.TakeRedItem(item);
    }

    private void UseItemOn(Card item, Card target)
    {
        if (target == null)
            return;

        _commands.UseItem(item, target);
        PlayerStats.ReducePlayerMana(item);
    }

    public void Summon(Deck summonList)
    {
        foreach (var card in summonList)
            Summon(card);
    }

    public void Summon(Card card)
    {
        PlayerStats.ReducePlayerMana(card);
        _commands.Summon(card);
        if (card.IsCharge())
            card.Location = CardLocation.Table;
    }

    public void UseBlueItem(Card card)
    {
        _commands.UseItem(card);
        PlayerStats.ReducePlayerMana(card);
    }
}

internal class SummonerDecider
{
    private readonly Commander _commander;

    public SummonerDecider(Commander commander)
    {
        _commander = commander;
    }

    public void SummonCreatures()
    {
        var summonList = GetSummonList(_commander.Deck);
        _commander.Summon(summonList);
    }

    private Deck GetSummonList(Deck deck)
    {
        var tableCount = deck.MyTableCards().Count();
        var highestCardsThatCanBeSummoned = deck.SummonableCreatures().ToList();

        var cards = LookForCheapCards(tableCount, highestCardsThatCanBeSummoned);
        var expensiveCards = ExpensiveCards(tableCount, highestCardsThatCanBeSummoned);

        return expensiveCards.Score > cards.Score ? expensiveCards : cards;
    }

    private Deck ExpensiveCards(int tableCount, List<Card> highestCardsThatCanBeSummoned)
    {
        var expensiveCards = new Deck(_commander.PlayerStats);
        var playerMana = _commander.PlayerStats.Mana;
        foreach (var card in highestCardsThatCanBeSummoned)
        {
            if (card.Cost <= playerMana && NotAboveSummonLimit(tableCount, expensiveCards))
            {
                playerMana -= card.Cost;
                expensiveCards.Add(card);
            }
            else
                break;
        }

        return expensiveCards;
    }

    private Deck LookForCheapCards(int tableCount, List<Card> highestCardsThatCanBeSummoned)
    {
        var mana = _commander.PlayerStats.Mana;
        var cards = new Deck(_commander.PlayerStats);
        foreach (var card in highestCardsThatCanBeSummoned.OrderBy(p => p.Cost))
        {
            if (card.Cost <= mana && NotAboveSummonLimit(tableCount, cards))
            {
                mana -= card.Cost;
                cards.Add(card);
            }
            else
                break;
        }

        return cards;
    }

    private bool NotAboveSummonLimit(int tableCount, Deck cards)
    {
        return tableCount + cards.Count < SimpleAttackStrategy.SummoningLimit;
    }
}

internal class SimpleAttackStrategy
{
    public const int SummoningLimit = 6;
    public readonly Commander _commander;
    public Commands _commands;
    private Deck _deck;
    private int _turnCount;
    private readonly SummonerDecider _summonerDecider;
    private Commands _canKillCommands;
    private int _canKillTotalCost = 0;

    public SimpleAttackStrategy(Commander commander)
    {
        _commander = commander;
        _summonerDecider = new SummonerDecider(_commander);
    }

    public List<string> SequentialPlay(Deck deck, int turnCount, Commands commands)
    {
        _turnCount = turnCount;
        _deck = deck;
        _commands = commands;

        var analysisResult = CanWeKillTheEnemy(deck);
        if (analysisResult.CanKill)
        {
            foreach (var attackingCard in _deck.MyTableCards())
                analysisResult.Commands.AttackEnemy(attackingCard);

            return analysisResult.Commands;
        }

        _summonerDecider.SummonCreatures();
        UseRedItems();
        UseGreenItems(deck);
        UseBlueItems();
        Attack(deck);
        return _commands;
    }

    public class AnalysisResult
    {
        public AnalysisResult(bool canKill)
        {
            CanKill = canKill;
        }

        public AnalysisResult(bool canKill, Commands commands)
        {
            CanKill = canKill;
            Commands = commands;
        }

        public bool CanKill { get; set; }
        public Commands Commands { get; set; }
    }


    #region CanWeKill
    private AnalysisResult CanWeKillTheEnemy(Deck deck)
    {
        _canKillTotalCost = 0;
        _canKillCommands = new Commands();
        var enemyGuards = deck.EnemyCards().Where(c => c.IsGuard).ToList();

        if (enemyGuards.Any(c => c.IsGuard) && !CanWeRemoveGuardsEasily(enemyGuards))
            return new AnalysisResult(false);

        int remainingEnemyLife = _commander.Enemy.Life - deck.MyTableCards().Sum(p => p.Attack);


        var totalDamage = 0;
        var items = deck.MyAffordableHandCards().Where(p => p.CardType != CardType.Creature).OrderBy(p => p.Cost);
        foreach (var item in items)
        {
            if (item.CardType == CardType.GreenItem && item.Attack > 0)
            {
                if (item.Cost + _canKillTotalCost > _commander.PlayerStats.Mana)
                {
                    Console.Error.WriteLine($"Can't kill enemy {remainingEnemyLife}");
                    return new AnalysisResult(false);
                }

                _canKillTotalCost += item.Cost;
                totalDamage += item.OpponentHealthChange * -1 + item.Attack;

                var target = deck.MyTableCards().First();
                _canKillCommands.UseItem(item, target);

                if (totalDamage >= remainingEnemyLife)
                    return new AnalysisResult(true, _canKillCommands);

            }
        }

        Console.Error.WriteLine($"Can't kill enemy {remainingEnemyLife}");

        return new AnalysisResult(false);
    }

    private bool CanWeRemoveGuardsEasily(List<Card> enemyGuards)
    {
        return enemyGuards.All(EasyRemove);
    }

    private bool EasyRemove(Card target)
    {
        var guardRemover = _deck.UsableRedItems().OrderBy(p => p.Cost).FirstOrDefault(c => c.IsGuard);
        if (guardRemover != null)
        {
            _canKillTotalCost += guardRemover.Cost;
            _canKillCommands.UseItem(guardRemover, target);
            _commander.UseRedItem(guardRemover, target);
            return true;
        }

        return false;
    }

    #endregion

    private void UseGreenItems(Deck deck)
    {
        foreach (var item in deck.Where(p => p.CardType == CardType.GreenItem && _commander.PlayerStats.Mana >= p.Cost))
        {
            UseGreenItem(item);
        }
    }

    private void UseGreenItem(Card item)
    {
        if (item.IsCharge())
        {
            var target = _deck.MyHandCards().Where(p => p.Cost > _turnCount).OrderBy(p => p.Cost).ThenByDescending(p => p.Attack).FirstOrDefault();
            _commander.UseGreenItem(target, item);
        }
        else
        {
            var target = _deck.MyTableCards().OrderByDescending(p => p.IsGuard)
                .ThenByDescending(p => p.Defense).FirstOrDefault();
            _commander.UseGreenItem(target, item);
        }
    }

    private void UseRedItems()
    {
        foreach (var redItem in _deck.UsableRedItems())
        {
            if (redItem.Cost == 0 && _turnCount < 3)
                continue;

            UseRedItem(redItem);
        }
    }

    private void UseRedItem(Card redItem)
    {
        if (redItem.IsGuard)
        {
            var guardEnemies = _deck.EnemyCards().Where(p => p.IsGuard).OrderByDescending(p => p.Defense);
            var target = guardEnemies.FirstOrDefault();

            if (target != null)
                _commander.UseRedItem(redItem, target);
        }
        else if (redItem.Defense < 0)
        {
            var target = _deck.EnemyCards().Where(p => p.Defense + redItem.Defense <= 0).OrderByDescending(p => p.Attack).FirstOrDefault();
            _commander.UseRedItem(redItem, target);
        }
        else
        {
            var target = _deck.EnemyCards().OrderByDescending(p => p.Defense).FirstOrDefault();
            _commander.UseRedItem(redItem, target);
        }
    }

    private void UseBlueItems()
    {
        foreach (var card in _deck.AffordableBlueItems())
            _commander.UseBlueItem(card);
    }

    private void Attack(Deck deck)
    {
        var attackingCards = deck.MyTableCards().ToList();
        var anyGuard = deck.EnemyCards().Any(c => c.IsGuard);

        if (!anyGuard && _commander.Enemy.Life < attackingCards.Sum(p => p.Attack))
        {
            AttackTheEnemyWith(attackingCards);
            return;
        }

        FocusGuardCards(deck, attackingCards);
        AttackAfter(attackingCards);
    }

    private void AttackTheEnemyWith(List<Card> attackingCards)
    {
        foreach (var attackingCard in attackingCards)
            _commands.AttackEnemy(attackingCard);
    }

    private void FocusGuardCards(Deck deck, List<Card> attackingCards)
    {
        foreach (var defendingCard in EnemyGuardCards(deck))
        {
            foreach (var attackingCard in attackingCards.Where(p => p.Attack > 0).OrderBy(p => p.Attack))
            {
                if (Attack(attackingCards, defendingCard, attackingCard))
                    break;
            }
        }
    }

    private void AttackAfter(List<Card> attackingCards)
    {
        foreach (var attackingCard in attackingCards)
        {
            if (attackingCard.IsLethal())
                AttackWithLetal(attackingCard);
            else
                _commands.AttackEnemy(attackingCard);
        }

        foreach (var card in _deck.EnemyCards().OrderBy(p => p.Attack))
        {
            Console.Error.WriteLine($"Potential damage {card.Attack} from: {card.InstanceId}");
        }
    }

    private void AttackWithLetal(Card attackingCard)
    {
        var target = _deck.EnemyCards()
            .OrderByDescending(p => p.Attack + p.GetAbilitiesScoreAsTarget()).FirstOrDefault();

        if (target != null)
        {
            var cardDies = target.Attack >= attackingCard.Defense;
            if (cardDies && target.Attack < 5)
            {
                _commands.AttackEnemy(attackingCard); ;
                return;
            }

            Console.Error.WriteLine($"Card here target {target.InstanceId}");

            target.Location = CardLocation.Dead;
            _commands.AttackCard(attackingCard, target);
        }
        else
            _commands.AttackEnemy(attackingCard);
    }

    private bool Attack(List<Card> attackingCards, Card defendingCard, Card attackingCard)
    {
        defendingCard.Defense -= attackingCard.Attack;
        _commands.Add($"ATTACK {attackingCard.InstanceId} {defendingCard.InstanceId}");
        attackingCards.Remove(attackingCard);

        if (defendingCard.Defense <= 0)
        {
            defendingCard.Location = CardLocation.Dead;
            return true;
        }

        return false;
    }

    private static IOrderedEnumerable<Card> EnemyGuardCards(Deck deck)
    {
        return deck.EnemyCards().Where(p => p.IsGuard)
            .OrderBy(p => p.Defense).ThenBy(p => p.Attack);
    }
}

internal class Deck : List<Card>
{
    private readonly PlayerStats _playerStats;

    public Deck(PlayerStats playerStats)
    {
        _playerStats = playerStats;
    }

    public IOrderedEnumerable<Card> SummonableCreatures()
    {
        return MyAffordableHandCards().Where(p => p.CardType == CardType.Creature)
            .OrderByDescending(p => p.Cost);
    }

    public IEnumerable<Card> MyAffordableHandCards()
    {
        return this.Where(p => p.Location == CardLocation.MyHand && p.Cost <= _playerStats.Mana);
    }

    public IEnumerable<Card> EnemyCards()
    {
        return this.Where(p => p.Location == CardLocation.OponentsTable);
    }

    public decimal Score => this.Sum(p => p.SummoningScore);

    public IEnumerable<Card> MyTableCards()
    {
        return this.Where(p => p.Location == CardLocation.Table);
    }

    public IEnumerable<Card> UsableRedItems()
    {
        return MyAffordableHandCards().Where(p => p.CardType == CardType.RedItem);
    }

    public IEnumerable<Card> AffordableBlueItems()
    {
        return MyAffordableHandCards().Where(p => p.CardType == CardType.BlueItem);
    }

    public IEnumerable<Card> MyHandCards()
    {
        return this.Where(p => p.Location == CardLocation.MyHand);
    }
}
#region Card

internal class Card
{
    private const char NonAbility = '-';
    public int Index { get; }

    public Card(string[] inputs, int i)
    {
        Index = i;
        int cardNumber = Int32.Parse(inputs[0]);
        InstanceId = Int32.Parse(inputs[1]);
        Location = (CardLocation)Int32.Parse(inputs[2]);
        CardType = (CardType)Int32.Parse(inputs[3]);
        Cost = Int32.Parse(inputs[4]);
        Attack = Int32.Parse(inputs[5]);
        Defense = Int32.Parse(inputs[6]);
        Abilities = inputs[7];
        MyHealthChange = Int32.Parse(inputs[8]);
        OpponentHealthChange = Int32.Parse(inputs[9]);
        CardDraw = Int32.Parse(inputs[10]);
    }

    public int CardDraw { get; set; }

    public int OpponentHealthChange { get; set; }

    public int MyHealthChange { get; set; }

    public CardType CardType { get; set; }

    public int Defense { get; set; }

    public string Abilities { get; set; }

    public int InstanceId { get; }

    public CardLocation Location { get; set; }

    public int Cost { get; }

    public int Attack { get; set; }

    public decimal Score
    {
        get
        {
            var costReduction = Cost * 2;
            return ScoreWithoutCost() - costReduction;
        }
    }

    private decimal ScoreWithoutCost()
    {
        var abilities = GetAbilitiesScore();
        var cardTypeBonus = CardType == CardType.Creature ? 0 : 1;
        var healthChanges = MyHealthChange + OpponentHealthChange * -1;
        var attack = Attack * 1.5m;
        if (attack == 0)
            attack = -2;

        return cardTypeBonus + attack + Defense + abilities + CardDraw + healthChanges;
    }

    public int GetAbilitiesScore()
    {
        int score = 0;
        if (IsGuard)
            score += 2;
        if (IsBreakthrough())
            score += 2;
        if (IsWard())
            score += 2;
        if (IsDrain())
            score += 3;
        if (IsLethal())
            score += 3;
        if (IsCharge())
            score += 3;

        return score;
    }

    public bool IsGuard => Abilities.Contains("G");
    public decimal SummoningScore => ScoreWithoutCost();

    public bool IsWard() => Abilities.Contains("W");
    public bool IsLethal() => Abilities.Contains("L");
    public bool IsBreakthrough() => Abilities.Contains("B");
    public bool IsDrain() => Abilities.Contains("D");
    public bool IsCharge() => Abilities.Contains("C");

    public int GetAbilitiesScoreAsTarget()
    {
        int score = 0;
        if (IsDrain())
            score += 5;
        return score;
    }

    public void RemoveAbilities(Card redItem)
    {
        foreach (var ability in redItem.Abilities.Where(c => c != NonAbility))
        {
            Abilities = Abilities.Replace(ability, NonAbility);
        }
    }

    public void AddStats(Card item)
    {
        Attack = item.Attack;
        Defense = item.Defense;
        if (item.AnyAbility())
            AddAbiities(item);
    }

    private void AddAbiities(Card item)
    {
        foreach (var ability in item.Abilities.Where(p => p != NonAbility))
        {
            if (!Abilities.Contains(ability))
                Abilities += ability;
        }
    }

    private bool AnyAbility()
    {
        return Abilities.Any(p => p != NonAbility);
    }

    public void TakeRedItem(Card item)
    {
        RemoveAbilities(item);

        if (item.Defense * -1 >= Defense)
            Location = CardLocation.Dead;
    }
}

#endregion

internal enum CardType
{
    Creature = 0,
    GreenItem = 1,
    RedItem = 2,
    BlueItem = 3
}

internal enum CardLocation
{
    MyHand = 0,
    Table = 1,
    OponentsTable = -1,
    Dead = -2
}

internal class Commands : List<string>
{
    public static void ExecuteCommands(List<string> commands)
    {
        var command = commands.Any() ? string.Join(";", commands) : "PASS";
        Console.WriteLine(command);
    }

    public void Summon(Card card)
    {
        Add($"SUMMON {card.InstanceId}");
    }

    public void UseItem(Card card, Card target)
    {
        Add($"USE {card.InstanceId} {target.InstanceId}");
    }

    public void AttackEnemy(Card attackingCard)
    {
        Add($"ATTACK {attackingCard.InstanceId} {-1}");
    }

    public void AttackCard(Card attackingCard, Card target)
    {
        Add($"ATTACK {attackingCard.InstanceId} {target.InstanceId}");
    }

    internal void UseItem(Card card)
    {
        Add($"USE {card.InstanceId} -1");
    }
}

internal class PlayerStats
{
    public PlayerStats(string[] inputs)
    {
        Life = int.Parse(inputs[0]);
        Mana = int.Parse(inputs[1]);
        int playerDeck = int.Parse(inputs[2]);
        int playerRune = int.Parse(inputs[3]);
    }

    public int Mana { get; set; }

    public int Life { get; set; }

    public void ReducePlayerMana(Card card)
    {
        Mana -= card.Cost;
    }
}
