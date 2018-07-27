using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    private static PlayerStats _playerStats;
    private static int _turnCount;

    static void Main(string[] args)
    {
        string[] inputs;
        _turnCount = 0;
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
                var player = new PlayerStats(inputs);
            }
            int opponentHand = int.Parse(Console.ReadLine());
            int cardCount = int.Parse(Console.ReadLine());


            var cards = new Cards();
            for (int i = 0; i < cardCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');

                cards.Add(new Card(inputs, i));
            }

            if (_turnCount < 30)
            {
                ChoosePick(cards);
            }
            else
            {
                PlayTurn(cards);
            }

            _turnCount++;
        }
    }

    private static void ChoosePick(Cards cards)
    {
        if (_turnCount > 10)
        {
            var cheapestCard = cards.OrderBy(p => p.Cost).First();
            Console.WriteLine($"PICK {cheapestCard.Index}");
        }
        else if (_turnCount >= 10 && _turnCount < 20)
        {
            var middleCard = cards.Where(p => p.Cost >= 4 && p.Cost <= 8).FirstOrDefault();
            if (middleCard == null)
                middleCard = cards.OrderByDescending(p => p.Cost).First();
            Console.WriteLine($"PICK {middleCard.Index}");
        }
        else
        {
            var cheapestCard = cards.OrderByDescending(p => p.Cost).First();
            Console.WriteLine($"PICK {cheapestCard.Index}");
        }
    }

    private static void PlayTurn(Cards cards)
    {

        var commands = new List<string>();

        SummonCards(cards, commands);

        Attack(cards, commands);

        if (commands.Any())
        {
            var finalExecution = string.Join(";", commands);
            Console.Error.WriteLine(finalExecution);
            Console.WriteLine(finalExecution);
        }
        else
        {
            Console.WriteLine("PASS");
        }
    }

    private static void Attack(Cards cards, List<string> commands)
    {
        foreach (var attackingCard in cards.Where(c => c.Location == 1))
        {
            var guardCard = cards.Where(p => p.Location == -1 && p.IsGuard && p.Defense > 0).FirstOrDefault();
            var targetId = -1;
            if (guardCard != null)
            {
                guardCard.Defense = guardCard.Defense - attackingCard.Attack;
                targetId = guardCard.InstanceId;
            }
            
            commands.Add($"ATTACK {attackingCard.InstanceId} {targetId}");
        }
    }

    private static void SummonCards(Cards cards, List<string> commands)
    {
        var highestCardThatCanBeSummoned = cards.Where(p => p.Location == 0 && p.Cost <= _playerStats.PlayerMana).OrderByDescending(p => p.Cost).FirstOrDefault();

        if (highestCardThatCanBeSummoned != null)
        {
            commands.Add($"SUMMON {highestCardThatCanBeSummoned.InstanceId}");
            if (highestCardThatCanBeSummoned.IsCharge())
            {
                highestCardThatCanBeSummoned.Location = 0;
            }
        }
    }
}

internal class PlayerStats
{
    public int PlayerMana { get; }

    public PlayerStats(string[] inputs)
    {
        int playerHealth = int.Parse(inputs[0]);
        PlayerMana = int.Parse(inputs[1]);
        int playerDeck = int.Parse(inputs[2]);
        int playerRune = int.Parse(inputs[3]);
    }
}

internal class Cards : List<Card>
{
}

internal class Card
{
    public int Index { get; }

    public Card(string[] inputs, int i)
    {
        Index = i;
        int cardNumber = int.Parse(inputs[0]);
        InstanceId = int.Parse(inputs[1]);
        Location = int.Parse(inputs[2]);
        int cardType = int.Parse(inputs[3]);
        Cost = int.Parse(inputs[4]);
        Attack = int.Parse(inputs[5]);
        Defense = int.Parse(inputs[6]);
        Abilities = inputs[7];
        int myHealthChange = int.Parse(inputs[8]);
        int opponentHealthChange = int.Parse(inputs[9]);
        int cardDraw = int.Parse(inputs[10]);
    }

    public int Defense { get; set; }

    public string Abilities { get; set; }

    public int InstanceId { get; }

    public int Location { get; set; }

    public int Cost { get; }
    public bool IsGuard => Abilities.Contains("G");
    public int Attack { get; set; }

    public bool IsCharge()
    {
        return Abilities.Contains("C");
    }
}