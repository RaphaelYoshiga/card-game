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
    private static int _draftCounter;
    private static PlayerStats _player;
    private static PlayerStats _enemy;

    static void Main(string[] args)
    {
        string[] inputs;
        _draftCounter = 0;
        // game loop
        while (true)
        {
            SetupPlayers();

            var cards = new Cards();
            int opponentHand = int.Parse(Console.ReadLine());
            int cardCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < cardCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                var card = new Card(inputs);
                cards.Add(card);
            }

            if (IsDraftPhase())
                DraftPhase(cards);
            else
                ActForTurn(cards);
        }
    }

    private static void SetupPlayers()
    {
        for (int i = 0; i < 2; i++)
        {
            var inputs = Console.ReadLine().Split(' ');
            if (i == 0)
                _player = new PlayerStats(inputs);
            else
                _enemy = new PlayerStats(inputs);
        }
    }

    private static void ActForTurn(Cards cards)
    {
        var commands = new List<string>();
        var card = cards.CardsInHand().OrderBy(c => c.Cost).First();
        if (card.Cost <= _player.Mana)
        {
            Console.Error.WriteLine("Test piggo");
            commands.Add($"SUMMON {card.InstanceId}");
        }

        foreach (var attackingCard in cards.Where(c => c.Location == 1))
        {
            commands.Add($"ATTACK {attackingCard.InstanceId} -1");
        }

        var finalExecution = string.Join(";", commands);


        if (commands.Any())
        {
            Console.Error.WriteLine(finalExecution);
            Console.WriteLine(finalExecution);
        }
        else
        {
            Console.WriteLine("PASS");
        }
    }

    private static bool IsDraftPhase()
    {
        return _draftCounter < 30;
    }

    private static void DraftPhase(List<Card> cards)
    {
        while (IsDraftPhase())
        {
            Console.Error.WriteLine($"{cards.Count}");
            var cardsToChoose = cards.Skip(_draftCounter).Take(3);

            var card = cardsToChoose.OrderBy(p => p.Cost).FirstOrDefault();

            if (card != null)
                Console.WriteLine($"PICK {card.CardDraw}");
            else
            {
                Console.WriteLine("PICK 0");
            }
            _draftCounter++;

        }
    }


}


public class PlayerStats
{
    public PlayerStats(string[] inputs)
    {
        Health = int.Parse(inputs[0]);
        Mana = int.Parse(inputs[1]);
        Deck = int.Parse(inputs[2]);
        Rune = int.Parse(inputs[3]);
    }

    public int Health { get; set; }

    public int Mana { get; set; }

    public int Deck { get; set; }

    public int Rune { get; set; }
}


public class Cards : List<Card>
{
    public IEnumerable<Card> CardsInHand()
    {
        return this.Where(c => c.Location == 0);
    }

}

public class Card
{
    public Card(string[] inputs)
    {
        CardNumber = int.Parse(inputs[0]);
        InstanceId = int.Parse(inputs[1]);
        Location = int.Parse(inputs[2]);
        int cardType = int.Parse(inputs[3]);
        Cost = int.Parse(inputs[4]);
        Attack = int.Parse(inputs[5]);
        Defense = int.Parse(inputs[6]);
        string abilities = inputs[7];
        MyHealthChange = int.Parse(inputs[8]);
        OpponentHealthChange = int.Parse(inputs[9]);
        CardDraw = int.Parse(inputs[10]);
    }

    public int InstanceId { get; set; }

    public int CardNumber { get; set; }

    public int Location { get; set; }

    public int Defense { get; set; }

    public int Attack { get; set; }

    public int CardDraw { get; set; }

    public int OpponentHealthChange { get; set; }

    public int MyHealthChange { get; set; }

    public int Cost { get; set; }
}
