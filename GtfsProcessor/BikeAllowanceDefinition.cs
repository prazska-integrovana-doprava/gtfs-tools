using AswModel.Extended;
using GtfsModel.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Rozhoduje o možnostech přepravy kol na spojích PID. Umí říci pro konkrétní spoj i pro konkrétní zastávku konkrétního spoje, což je 
    /// podstatné zejména tam, kde se podmínky liší po trase.
    /// </summary>
    class BikeAllowanceDefinition
    {
        public class TramNodeChain
        {
            public int[] NodeList { get; set; }
            public bool IsLastStopMandatory { get; set; }
            
            public TramNodeChain(IEnumerable<int> nodes, bool isLastStopMandatory = false)
            {
                NodeList = nodes.ToArray();
                IsLastStopMandatory = isLastStopMandatory;
            }

            public bool IsChainSatisfied(int currentNode, IEnumerable<int> nextNodes)
            {
                if (!nextNodes.Any())
                    return false;

                for (int i = 0; i < NodeList.Length - 1; i++)
                {
                    if (NodeList[i] == currentNode)
                    {
                        // podíváme se na 3 následující v řetězci a jestli tam jedeme (stačí trefit jeden), je to OK
                        for (int j = i + 1; j < NodeList.Length && j <= i + 3; j++)
                        {
                            if (NodeList[j] == nextNodes.First())
                            {
                                if (IsLastStopMandatory && !nextNodes.Contains(NodeList.Last()))
                                    return false; // spoj nejede přes poslední zastávku v řetězci => porušení podmínky
                                else
                                    return true;
                            }
                        }

                        return false;
                    }
                }

                return false;
            }
        }

        public List<TramNodeChain> TramNodeChains = new List<TramNodeChain>()
        {
            new TramNodeChain(new [] { 163, 321, 350, 906, 126 }), // Hradčanská - Podbaba
            new TramNodeChain(new [] { 321, 757, 130, 40, 157, 943, 73, 462, 283, 868, 85 }), // Vítězné náměstí - Divoká Šárka
            new TramNodeChain(new [] { 163, 592, 867, 645, 519, 15, 858, 844, 507, 541 }), // Hradčanská - Petřiny
            new TramNodeChain(new [] { 867, 140, 362, 366, 103, 778, 994, 636, 872, 610, 364, 31 }), // Vozovna Střešovice - Bílá Hora
            new TramNodeChain(new [] { 46, 531, 362 }), // Brusnice - Malovanka
            new TramNodeChain(new [] { 1040, 25, 805, 254, 240, 289, 580, 567, 865, 394, 395, 113, 236, 47 }), // Anděl - Sídliště Řepy
            new TramNodeChain(new [] { 458, 337, 914, 147, 49, 1049, 1050, 1030, 1019}), // Smíchovské nádraží - Barrandov
            new TramNodeChain(new [] { 449, 71, 19, 76, 388, 1006, 929, 948, 947, 946, 945 }), // Nádraží Braník - Levského
            new TramNodeChain(new [] { 482, 503, 530, 597, 2784, 433, 866 }), // Náměstí Bratří Synků - Vozovna Pankrác
            new TramNodeChain(new [] { 2784, 385 }), // Kotorská - Pankrác
            new TramNodeChain(new [] { 482, 152, 559, 376, 550, 183, 751, 697 }), // Náměstí Bratří Synků - Spořilov
            new TramNodeChain(new [] { 921, 849, 299, 1108 }, true), // Želivského - Vinice (jen spoje, které opravdu jedou směr ÚDDP, ne ty co by pak mohly jet na Kubáň)
            new TramNodeChain(new [] { 921, 849, 299, 848, 713, 599 }, true), // Želivského - Na Hroudě (jen spoje, které opravdu jedou ze Strašnické směr Hostivař, ne ty co jedou na Kubáň)
            new TramNodeChain(new [] { 1108, 690, 900, 496, 70, 1071, 358, 404, 807 }), // Vinice - ÚDDP
            new TramNodeChain(new [] { 599, 459, 618, 584, 673, 889, 670, 596, 403, 472, 453 }), // Na Hroudě - Nádraží Hostivař
            new TramNodeChain(new [] { 529, 1074, 512, 837 }, true), // Palmovka - Vozovna Žižkov (jen spoje které opravdu jedou z Ohrady doleva a ne na NNŽ)
            new TramNodeChain(new [] { 512, 837, 714, 180, 266, 694 }), // Ohrada - Spojovací
            new TramNodeChain(new [] { 529, 12, 1075, 348, 134, 1401, 691, 775, 755, 242, 135, 652, 72 }), // Palmovka - Libeň - Lehovec
            new TramNodeChain(new [] { 12, 308, 464, 474, 1052, 873, 75, 499, 1053, 144, 242 }), // Balabenka - Vysočany - Kbelská
            new TramNodeChain(new [] { 529, 471 ,779, 861, 54, 570, 514, 249, 675, 676, 314, 78, 740, 651 }), // Palmovka - Ďáblice
            new TramNodeChain(new [] { 115, 765, 447, 139, 249, 675, 718, 333, 864 }), // Nádraží Holešovice - Vozovna Kobylisy
        };

        /// <summary>
        /// Vrací info o možnosti přepravy kol na spoji v dané zastávce. Určuje se podle druhu dopravy, u tramvají rozhoduje i čas.
        /// </summary>
        /// <param name="trafficType"></param>
        /// <returns></returns>
        public BikeAccessibility GetBikesAllowedForStopTime(AswTrafficType trafficType, string routeShortName, Remark[] stopTimeRemarks, int currentStopNodeNumber, IEnumerable<int> nextNodeNumbers, BikeAccessibility? prevStopTimeBikes)
        {
            if (trafficType == AswTrafficType.Metro || trafficType == AswTrafficType.Funicular || trafficType == AswTrafficType.Ferry || trafficType == AswTrafficType.Rail)
            {
                return BikeAccessibility.Possible;
            }
            else if (trafficType == AswTrafficType.Bus || trafficType == AswTrafficType.Trolleybus)
            {
                if (routeShortName == "Cyklobus")
                {
                    return BikeAccessibility.Possible;
                }
                else if (routeShortName == "147")
                {
                    if (stopTimeRemarks.Any(r => r.Symbol1 == "/L/" && r.Text.Contains("Možnost přepravy jízdních kol")))
                    {
                        return BikeAccessibility.PickupOnly;
                    }
                    else if (prevStopTimeBikes.HasValue && prevStopTimeBikes != BikeAccessibility.NotPossible)
                    {
                        if (currentStopNodeNumber == 721 || currentStopNodeNumber == 869)
                        {
                            return BikeAccessibility.DropOffOnly;
                        }
                        else
                        {
                            return BikeAccessibility.AllowedToStayOnBoard;
                        }
                    }
                    else
                    {
                        return BikeAccessibility.NotPossible;
                    }
                }
                else
                {
                    return BikeAccessibility.NotPossible;
                }
            }
            else if (trafficType == AswTrafficType.Tram)
            {
                // u tramvají si zjednodušíme práci tím, že využijeme znalosti, že jakmile je nástup s kolem povolen, je už povolen do konce trasy...
                // zároveň si tím řešíme neschopnost určit bike allowance pro konečnou zastávku v případě, kdy není zároveň konečnou zastávkou node chainu
                if (prevStopTimeBikes == BikeAccessibility.Possible)
                    return BikeAccessibility.Possible;
                else
                    return TramNodeChains.Any(tmc => tmc.IsChainSatisfied(currentStopNodeNumber, nextNodeNumbers)) ? BikeAccessibility.Possible : BikeAccessibility.NotPossible;
            }
            else
            {
                return BikeAccessibility.Unknown;
            }
        }

        /// <summary>
        /// Agreguje možnost převozu kol pro celý spoj podle jednotlivých stoptimes podle následujících pravidel.
        /// 
        /// Pokud na všech zastávkách je možná přeprava kol, vrací že možno.
        /// Pokud na všech zastávkách není možná přeprava kol, vrací že ne možno.
        /// V ostatních případech vrací že neurčeno.
        /// </summary>
        /// <param name="stopTimeAccessibility">Možnosti převozu kol v jednotlivých zastávkách</param>
        /// <returns></returns>
        public BikeAccessibility SetBikesAllowedForTrip(IEnumerable<BikeAccessibility> stopTimeAccessibility)
        {
            var accessibilities = stopTimeAccessibility.ToArray();
            if (accessibilities.All(a => a == BikeAccessibility.Possible) 
                || accessibilities.Skip(1).All(a => a == BikeAccessibility.Possible) && accessibilities.First() == BikeAccessibility.PickupOnly
                || accessibilities.Take(accessibilities.Length - 1).All(a => a == BikeAccessibility.Possible) && accessibilities.Last() == BikeAccessibility.DropOffOnly)
            {
                // vše possible, případně tolerujeme u první zastávky pouze nástup a u poslední pouze výstup
                return BikeAccessibility.Possible;
            }
            else if (accessibilities.All(a => a == BikeAccessibility.NotPossible || a == BikeAccessibility.AllowedToStayOnBoard || a == BikeAccessibility.PickupOnly)
                || accessibilities.All(a => a == BikeAccessibility.NotPossible || a == BikeAccessibility.AllowedToStayOnBoard || a == BikeAccessibility.DropOffOnly))
            {
                // dvě možnosti, buď neexistuje žádná zastávka umožňující nástup s kolem, nebo neexistuje žádná zastávka umožňující výstup s kolem
                // případ, že sice je umožněn nástup, ale na trase až za výstupem neřešíme, ale dalo by se, kdyby někdo chtěl
                return BikeAccessibility.NotPossible;
            }
            else
            {
                return BikeAccessibility.Unknown;
            }
        }
    }
}
