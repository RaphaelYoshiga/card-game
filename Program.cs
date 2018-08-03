using System;
using System.Linq;
using System.Collections.Generic;

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
                {
                    _playerStats = new PlayerStats(inputs);
                }

                _enemyStats = new PlayerStats(inputs);
            }

            int opponentHand = int.Parse(Console.ReadLine());
            int cardCount = int.Parse(Console.ReadLine());

            var cards = new Deck();
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
    private readonly Deck _deck;

    public CardSelector()
    {
        _deck = new Deck();
    }

    public void ChoosePick(Deck cardsChoice)
    {
        var highCostCard = _deck.Count(p => p.Cost > 7);
        if (highCostCard > 4)
        {
            var card = cardsChoice.Where(p => p.Cost <= 7).OrderByDescending(p => p.Score).First();
            Pick(card);
            return;
        }

        var bestCard = cardsChoice.OrderByDescending(p => p.Score).First();
        _deck.Add(bestCard);

        Pick(bestCard);
    }

    private static void Pick(Card card)
    {
        Console.WriteLine($"PICK {card.Index}");
    }
}


internal class Commander
{
    private readonly SimpleAttackStrategy _simpleAttackStrategy;
    private readonly CardSelector _cardSelector;
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
        var commands = _simpleAttackStrategy.SequentialPlay(deck, adjustTurnCount);

        CommandExecutor.ExecuteCommands(commands);
    }
}

internal class SimpleAttackStrategy
{
    private const int SummoningLimit = 6;
    private readonly Commander _commander;
    private CommandExecutor _commands;
    private Deck _deck;
    private int _turnCount;

    public SimpleAttackStrategy(Commander commander)
    {
        _commander = commander;
    }

    public List<string> SequentialPlay(Deck deck, int turnCount)
    {
        _turnCount = turnCount;
        _deck = deck;
        _commands = new CommandExecutor();
        SummonCreatures();
        UseGreenItems(deck);
        UseRedItems(deck);
        UseBlueItems(deck);
        Attack(deck);
        return _commands;
    }

    private void SummonCreatures()
    {
        if (GetSummonList(out var summonList)) return;

        foreach (var card in summonList)
        {
            _commander.PlayerStats.PlayerMana -= card.Cost;
            _commands.Summon(card);
            if (card.IsCharge())
                card.Location = CardLocation.Table;
        }
    }

    private bool GetSummonList(out Deck summonList)
    {
        var tableCount = _deck.MyTableCards().Count();
        var highestCardsThatCanBeSummoned = _deck.SummonableCreatures(_commander.PlayerStats.PlayerMana).ToList();

        var cards = LookForCheapCards(tableCount, highestCardsThatCanBeSummoned);
        var expensiveCards = ExpensiveCards(tableCount, highestCardsThatCanBeSummoned);

        Console.Error.WriteLine($" Expensive score vs cards score {expensiveCards.Score}, : {cards.Score}");
        summonList = expensiveCards.Score > cards.Score ? expensiveCards : cards;
        return false;
    }

    private Deck ExpensiveCards(int tableCount, List<Card> highestCardsThatCanBeSummoned)
    {
        var expensiveCards = new Deck();
        var playerMana = _commander.PlayerStats.PlayerMana;
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
        var fakeMana = _commander.PlayerStats.PlayerMana;
        var cards = new Deck();
        foreach (var card in highestCardsThatCanBeSummoned.OrderBy(p => p.Cost))
        {
            if (card.Cost <= fakeMana && NotAboveSummonLimit(tableCount, cards))
            {
                fakeMana -= card.Cost;
                cards.Add(card);
            }
            else
                break;
        }

        return cards;
    }

    private bool NotAboveSummonLimit(int tableCount, Deck cards)
    {
        return tableCount + cards.Count < SummoningLimit;
    }

    private void UseGreenItems(Deck deck)
    {
        foreach (var item in deck.Where(p => p.CardType == CardType.GreenItem && _commander.PlayerStats.PlayerMana >= p.Cost))
        {
            var target = deck.Where(p => p.Location == CardLocation.Table).OrderByDescending(p => p.IsGuard)
                .ThenByDescending(p => p.Defense).FirstOrDefault();
            if (target != null)
            {
                _commands.UseItem(item, target);
                _commander.PlayerStats.PlayerMana -= item.Cost;

                target.AddStats(item);
            }
        }
    }

    private void UseRedItems(Deck deck)
    {
        foreach (var redItem in _deck.UsableRedItems(deck, _commander.PlayerStats.PlayerMana))
        {
            if (redItem.Cost == 0 && _turnCount < 4)
                continue;

            UseRedItem(deck, redItem);
        }
    }

    private void UseRedItem(Deck deck, Card redItem)
    {
        if (redItem.IsGuard)
        {
            var guardEnemies = deck.MyOponentsTableCards().Where(p => p.IsGuard).OrderByDescending(p => p.Defense);
            var target = guardEnemies.FirstOrDefault();

            if (target != null)
                UseRedItem(target, redItem);
        }
        else
        {
            var target = deck.MyOponentsTableCards().OrderByDescending(p => p.Defense).FirstOrDefault();
            UseItemOn(target, redItem);
        }
    }

    private void UseRedItem(Card target, Card redItem)
    {
        UseItemOn(target, redItem);
        target.RemoveAbilities(redItem);

        if (redItem.Defense * -1 >= target.Defense)
            target.Location = CardLocation.Dead;
    }

    private void UseItemOn(Card target, Card card)
    {
        if (target == null)
            return;

        _commands.Add($"USE {card.InstanceId} {target.InstanceId}");
        _commander.PlayerStats.PlayerMana -= card.Cost;
    }

    private void UseBlueItems(Deck deck)
    {
        foreach (var card in deck.Where(p =>
            p.CardType == CardType.BlueItem && _commander.PlayerStats.PlayerMana >= p.Cost))
        {
            _commands.Add($"USE {card.InstanceId} -1");
            _commander.PlayerStats.PlayerMana -= card.Cost;
        }
    }

    private void Attack(Deck deck)
    {
        var attackingCards = deck.Where(p => p.Location == CardLocation.Table).ToList();

        FocusGuardCards(deck, attackingCards);
        FocusDrainCards(deck, attackingCards);
        AttackAfter(attackingCards);
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

    private void FocusDrainCards(Deck deck, List<Card> attackingCards)
    {
        foreach (var defendingCard in EnemyDrainCards(deck))
        {
            foreach (var attackingCard in attackingCards.Where(p => p.Attack > 0).OrderBy(p => p.Attack))
            {
                if (Attack(attackingCards, defendingCard, attackingCard))
                    break;
            }
        }
    }

    private IEnumerable<Card> EnemyDrainCards(Deck deck)
    {
        return deck.MyOponentsTableCards().Where(p => p.IsDrain());
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

        foreach (var card in _deck.MyOponentsTableCards().OrderBy(p => p.Attack))
        {
            Console.Error.WriteLine($"Potential damage {card.Attack} from: {card.InstanceId}");
        }
    }

    private void AttackWithLetal(Card attackingCard)
    {
        var target = _deck.MyOponentsTableCards()
            .OrderByDescending(p => p.Attack + p.GetAbilitiesScoreAsTarget()).FirstOrDefault();

        if (target != null)
        {
            var cardDies = target.Attack >= attackingCard.Defense;

            if (cardDies && target.Attack < 5)
            {
                _commands.AttackEnemy(attackingCard);;
                return;
            }

            Console.Error.WriteLine($"Card here target {target.InstanceId}");
            _commands.Add($"ATTACK {attackingCard.InstanceId} {target.InstanceId}");
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
        return deck.MyOponentsTableCards().Where(p => p.IsGuard)
            .OrderBy(p => p.Defense).ThenBy(p => p.Attack);
    }
}

internal class Deck : List<Card>
{
    public IOrderedEnumerable<Card> SummonableCreatures(int playerMana)
    {
        return MyHandCards(playerMana).Where(p => p.Cost <= playerMana && p.CardType == CardType.Creature)
            .OrderByDescending(p => p.Cost);
    }

    public IEnumerable<Card> MyHandCards(int playerMana)
    {
        return this.Where(p => p.Location == CardLocation.MyHand && p.Cost <= playerMana);
    }

    public IEnumerable<Card> MyOponentsTableCards()
    {
        return this.Where(p => p.Location == CardLocation.OponentsTable);
    }

    public int Score => this.Sum(p => p.SummoningScore);

    public IEnumerable<Card> MyTableCards()
    {
        return this.Where(p => p.Location == CardLocation.Table);
    }

    public IEnumerable<Card> UsableRedItems(Deck deck, int playerMana)
    {
        return deck.Where(p => p.CardType == CardType.RedItem && playerMana >= p.Cost);
    }
}

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

    public int Score
    {
        get
        {
            var costReduction = Cost * 2;
            return ScoreWithoutCost() - costReduction;
        }
    }

    private int ScoreWithoutCost()
    {
        var abilities = GetAbilitiesScore();
        var cardTypeBonus = CardType == CardType.Creature ? 0 : 1;
        var healthChanges = MyHealthChange + OpponentHealthChange * -1;
        return cardTypeBonus + Attack + Defense + abilities + CardDraw + healthChanges;
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
    public int SummoningScore => ScoreWithoutCost();

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
}

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

internal class CommandExecutor : List<string>
{
    public static void ExecuteCommands(List<string> commands)
    {
        var command = commands.Any() ? string.Join(";", commands) : "PASS";
        Console.WriteLine(command);
    }

    public void Summon(Card card)
    {
        this.Add($"SUMMON {card.InstanceId}");
    }

    public void UseItem(Card card, Card target)
    {
        this.Add($"USE {card.InstanceId} {target.InstanceId}");
    }

    public void AttackEnemy(Card attackingCard)
    {
        this.Add($"ATTACK {attackingCard.InstanceId} {-1}");
    }
}

internal class PlayerStats
{
    public PlayerStats(string[] inputs)
    {
        PlayerHealth = int.Parse(inputs[0]);
        PlayerMana = int.Parse(inputs[1]);
        int playerDeck = int.Parse(inputs[2]);
        int playerRune = int.Parse(inputs[3]);
    }

    public int PlayerMana { get; set; }

    public int PlayerHealth { get; set; }
}