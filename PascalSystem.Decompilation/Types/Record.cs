namespace PascalSystem.Decompilation.Types
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;

    public class Record : Structured
    {
        private static int nextId;
        private readonly List<string> names = new();
        private readonly List<Base> types = new();
        private readonly int id;
        private int nextGenericMemberName = 1;

        public Record(bool isGeneric = false)
        {
            this.IsGeneric = isGeneric;
            this.id = Record.nextId++;
            //this.AddType(new TypeSize((BitCount)16).Proxy(), "m0");
        }

        public int Count => this.types.Count;

        public bool IsGeneric { get; }

        public override BitCount Size => this.types.Aggregate((BitCount)0, (s, n) => s + n.Size);

        public void AddType(Base type, string name)
        {
            //var t = TypeBase.GetNamedType(type.ToCsString());
            //if (t != null)
            //    type = t;
            this.types.Add(type);
            this.names.Add(name);
        }

        public Base GetType(int index) => index < this.types.Count ? this.types[index] : Void.Instance;

        public Base GetType(string name)
        {
            var i = this.names.FindIndex(s => s == name);
            return i != -1 ? this.types[i] : throw new DecompilationException("bad name");
        }

        public string GetName(int index) => this.names[index];

        public Base? GetTypeAtOffset(BitCount offset)
        {
            foreach (var t in this.types)
            {
                if ((int)offset >= 0 && t.Size > offset || (int)t.Size == 0)
                    return t;
                offset -= t.Size;
            }
            return null;
        }

        public void SetTypeAtOffset(BitCount offset, Base type)
        {
            for (var i = 0; i < this.types.Count; i++)
            {
                if ((int)offset >= 0 && this.types[i].Size > offset)
                {
                    var oldsz = this.types[i].Size;
                    this.types[i] = type;
                    if (type.Size >= oldsz)
                        return;
                    this.types.Add(this.types[^1]);
                    this.names.Add(this.names[^1]);
                    for (var n = this.types.Count - 1; n > 1; n--)
                    {
                        this.types[n] = this.types[n - 1];
                        this.names[n] = this.names[n - 1];
                    }
                    this.types[i + 1] = new Size(oldsz - type.Size);
                    this.names[i + 1] = "pad";
                    return;
                }
                offset -= this.types[i].Size;
            }
            if ((int)offset > 0)
            {
                this.types.Add(new Size(offset));
                this.names.Add("pad");
            }
            this.types.Add(type);
            this.names.Add("undefined");
        }

        public string? GetNameAtOffset(BitCount offset)
        {
            for (var i = 0; i < this.types.Count; i++)
            {
                if ((int)offset == 0 || (int)offset >= 0 && this.types[i].Size > offset)
                    return this.names[i];
                offset -= this.types[i].Size;
            }
            return null;
        }

        public void SetNameAtOffset(BitCount offset, string name)
        {
            for (var i = 0; i < this.types.Count; i++)
            {
                if ((int)offset == 0 || (int)offset >= 0 && this.types[i].Size > offset)
                {
                    this.names[i] = name;
                    return;
                }
                offset -= this.types[i].Size;
            }
        }

        public void UpdateGenericMember(BitCount offset, Base type, ref bool hasChanged)
        {
            var existingType = this.GetTypeAtOffset(offset);
            if (existingType != null)
                existingType.MeetWith(type, ref hasChanged);
            else
            {
                this.SetTypeAtOffset(offset, type);
                this.SetNameAtOffset(offset, "member" + this.nextGenericMemberName++);
            }
        }

        public override Base Clone()
        {
            var t = this;
            t.names.Clear();
            t.types.Clear();
            for (var i = 0; i < this.types.Count; i++)
                t.AddType(this.types[i], this.names[i]);
            return t;
        }

        public override bool Equals(Base other) => other is Record otherCmp && otherCmp.types.Count == this.types.Count &&
                                                   !this.types.Where((t, i) => !t.Equals(otherCmp.types[i])).Any();

        public override string ToString() => "Record" + this.id;

        public bool IsSuperRecordOf(Base other)
        {
            if (other is not Record otherCmp)
                return false;
            var n = otherCmp.types.Count;
            if (n > this.types.Count)
                return false;
            for (var i = 0; i < n; i++)
                if (!otherCmp.types[i].Equals(this.types[i]))
                    return false;
            return true;
        }

        public bool IsSubRecordOf(Base other)
        {
            var otherCmp = other.As<Record>();
            //if (otherCmp == null)
            //    return false;
            var n = this.types.Count;
            if (n > otherCmp.types.Count)
                return false;
            for (var i = 0; i < n; i++)
                if (!otherCmp.types[i].IsCompatibleWith(this.types[i]))
                    return false;
            return true;
        }

        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>())
                return this;
            if (!other.ResolvesTo<Record>())
                return this.types[0].IsCompatibleWith(other)
                    ? this
                    : throw new DecompilationException("Could not meet");
            if (this.Equals(other))
                return this;
            var otherCmp = other.As<Record>();
            if (otherCmp.IsSuperRecordOf(this))
            {
                hasChanged = true;
                return other;
            }
            if (!otherCmp.IsSubRecordOf(this))
                throw new DecompilationException("Could not meet");
            hasChanged = true;
            return this;
        }

        public override bool IsCompatibleWith(Base other, bool testAllElements = false) => this.IsCompatible(other,
            testAllElements);

        public override bool IsCompatible(Base other, bool testAllElements)
        {
            if (other.ResolvesTo<Void>())
                return true;
            if (!other.ResolvesTo<Record>())
                return !testAllElements && this.types[0].IsCompatibleWith(other);
            var oc = other.As<Record>();
            if (this.IsSubRecordOf(other) || other.As<Record>().IsSubRecordOf(this))
                return true;
            var n = this.types.Count;
            if (n != oc.types.Count)
                return false;
            for (var i = 0; i < n; i++)
                if (!this.types[i].IsCompatibleWith(oc.types[i]))
                    return false;
            return true;
        }

        //private readonly Interval typeInterval = new Interval();

        //public override Base MeetAt(ByteCount offset, Base other)
        //{
        //    var member = this.typeInterval.MeetAt(offset, other);

        //    if (this.GetNameAtOffset(offset) == null)
        //        this.AddType(member, "m" + ((int)offset).ToString("X"));

        //    return member;
        //}

        //public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        //{
        //    if (!(other is Record c))
        //        return base.MeetWith(other, ref hasChanged, setToHighestPointer);

        //    var offset = (ByteCount)0;
        //    for (var x = 0; x < c.Count; x++)
        //    {
        //        var member = c.GetTypeAtOffset(offset);
        //        this.typeInterval.MeetAt(offset, member);
        //        offset += (ByteCount)member.Size;
        //    }
        //    return this;
        //}
    }
}