using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace CommonLibrary
{
    /// <summary>
    /// Reprezentace kalendáře jízd bitmapou nul a jedniček. Relativní, neobsahuje pevné datum začátku, obsahuje pouze reprezentaci dané bitmapy
    /// a pro určení konkrétních dnů je nutné dodat start date.
    /// </summary>
    public class ServiceDaysBitmap : IEnumerable<bool>
    {
        private bool[] bitmap;

        /// <summary>
        /// Délka bitové řady (počet dní)
        /// </summary>
        public int Length { get { return bitmap.Length; } }

        /// <summary>
        /// Vrací true, pokud je kalendář plný nul
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                foreach (var value in bitmap)
                {
                    if (value)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Vrátí informaci, zda je záznam dle kalendáře v <paramref name="index"/>-tý den feedu platný.
        /// Při indexaci mimo rozsah bitmapy vyhazuje <see cref="IndexOutOfRangeException"/>.
        /// </summary>
        /// <param name="index">Index dne od začátku feedu</param>
        /// <returns>True, pokud je záznam dle kalendáře v den platný, false pokud nikoliv</returns>
        /// <exception cref="IndexOutOfRangeException" />
        public bool this [int index]
        {
            get
            {
                if (index >= 0 && index < bitmap.Length)
                    return bitmap[index];
                else
                    throw new IndexOutOfRangeException("Wrong index to Service days bitmap");
            }
            set
            {
                bitmap[index] = value;
            }
        }

        /// <summary>
        /// Vytvoří bitmapu
        /// </summary>
        /// <param name="bitmap"></param>
        public ServiceDaysBitmap(bool[] bitmap)
        {
            this.bitmap = (bool[])bitmap.Clone();
        }

        /// <summary>
        /// Vytvoří kopii <see cref="bitmap"/>.
        /// </summary>
        /// <param name="bitmap"></param>
        public ServiceDaysBitmap(ServiceDaysBitmap bitmap)
            : this(bitmap.bitmap)
        {
        }

        /// <summary>
        /// Vrátí první den, kdy bitmapa platí. Speciální případ, pokud je bitmapa prázdná, tak vrací den před začátkem feedu (jako nejdřívější možnou variantu).
        /// </summary>
        /// <returns>Datum první jedničky</returns>
        public RelativeDate GetFirstDayOfService()
        {
            for (int i = 0; i < Length; i++)
            {
                if (bitmap[i])
                {
                    return i;
                }
            }

            // výchozí hodnota - pokud byly samé nuly, tak prostě chceme nejdřívější možnou variantu, což je před prvním dnem feedu
            return -1;
        }

        /// <summary>
        /// Vrátí poslední den, kdy bitmapa platí. Speciální případ, pokud je bitmapa prázdná, tak vrací den před začátkem feedu.
        /// </summary>
        /// <returns>Datum poslední jedničky</returns>
        public RelativeDate GetLastDayOfService()
        {
            for (int i = Length - 1; i >= 0; i--)
            {
                if (bitmap[i])
                {
                    return i;
                }
            }

            return -1;
        }

        public override int GetHashCode()
        {
            int result = Length;
            for (int i = 0; i < Length; i++)
            {
                result ^= (this[i] ? 1 : 0) << (i % 32);
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ServiceDaysBitmap;
            if (other == null)
                return false;

            if (Length != other.Length)
                return false;

            for (int i = 0; i < Length; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Posune řadu o jeden den zpět
        /// </summary>
        /// <returns>O 1 den kratší bitmapa s umazaným prvním záznamem</returns>
        public ServiceDaysBitmap ShiftLeft(int count = 1)
        {
            return new ServiceDaysBitmap(bitmap.Skip(count).ToArray());
        }

        /// <summary>
        /// Vrátí průnik bitových map
        /// </summary>
        /// <param name="other">Druhá bitová mapa</param>
        /// <returns>Průnik bitových map</returns>
        public ServiceDaysBitmap Intersect(ServiceDaysBitmap other)
        {
            var commonLength = Math.Min(Length, other.Length);
            var result = new bool[commonLength];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = this[i] && other[i];
            }

            return new ServiceDaysBitmap(result);
        }

        /// <summary>
        /// Vrátí sjednocení bitových map
        /// </summary>
        /// <param name="other">Druhá bitová mapa</param>
        /// <returns>Sjednocení bitových map</returns>
        public ServiceDaysBitmap Union(ServiceDaysBitmap other)
        {
            var commonLength = Math.Max(Length, other.Length);
            var result = new bool[commonLength];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (i >= Length ? false : this[i]) || (i >= other.Length ? false : other[i]);
            }

            return new ServiceDaysBitmap(result);
        }

        /// <summary>
        /// Vrátí rozdíl bitových map
        /// </summary>
        /// <param name="other">Druhá bitová mapa</param>
        public ServiceDaysBitmap Subtract(ServiceDaysBitmap other)
        {
            var result = new bool[Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = this[i] && !(i < other.Length && other[i]);
            }

            return new ServiceDaysBitmap(result);
        }

        /// <summary>
        /// Merge bitmap. Vyžaduje alespoň jednu položku, jinak háže výjimku.
        /// </summary>
        /// <param name="others">Alespoň jedna bitmapa</param>
        /// <returns>Merge bitmap</returns>
        public static ServiceDaysBitmap Union(IEnumerable<ServiceDaysBitmap> bitmaps)
        {
            var result = bitmaps.First();
            foreach (var bitmap in bitmaps.Skip(1))
            {
                result = result.Union(bitmap);
            }

            return result;
        }

        /// <summary>
        /// Vrací true, pokud je toto bitové pole podmnožinou jiného.
        /// </summary>
        /// <param name="other">Druhá bitová mapa</param>
        /// <returns>True, pokud je podmnožinou, jinak false.</returns>
        public bool IsSubsetOf(ServiceDaysBitmap other)
        {
            var commonLength = Math.Min(Length, other.Length);
            for (int i = 0; i < commonLength; i++)
            {
                if (this[i] && !other[i])
                    return false;
            }

            for (int i = commonLength; i < Length; i++)
            {
                if (this[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Načte z řetězce nul a jedniček. Při výjimce vyhazuje FormatException.
        /// </summary>
        /// <param name="startDate">Datum začátku</param>
        /// <param name="bitmapStr">Řetězec nul a jedniček</param>
        /// <returns>Načtená instance</returns>
        public static ServiceDaysBitmap FromBitmapString(string bitmapStr)
        { 
            var length = bitmapStr.Length;
            var bitmap = new bool[length];
            
            for (int i = 0; i < length; i++)
            {
                if (bitmapStr[i] != '0' && bitmapStr[i] != '1')
                {
                    throw new FormatException($"Kalendář jízd musí být složen pouze z nul a jedniček. Hodnota {bitmapStr} je neplatná.");
                }

                bool inService = bitmapStr[i] == '1';
                bitmap[i] = inService;
            }

            return new ServiceDaysBitmap(bitmap);
        }

        /// <summary>
        /// Vytvoří bitmapu zadané délky se samými jedničkami
        /// </summary>
        /// <param name="length">Délka bitmapy</param>
        public static ServiceDaysBitmap CreateAlwaysValidBitmap(int length)
        {
            return new ServiceDaysBitmap(Enumerable.Repeat(true, length).ToArray());
        }

        public IEnumerator<bool> GetEnumerator()
        {
            return bitmap.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return bitmap.GetEnumerator();
        }

        public override string ToString()
        {
            return new string(bitmap.Select(v => v ? '1' : '0').ToArray());
        }
    }
}
