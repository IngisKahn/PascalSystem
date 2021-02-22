namespace PascalSystem.Decompilation.Types
{
    using System.Text;
    using Model;

    public class Proxy : Base
    {
        public Base Value { get; private set; }

        public Proxy(Base value) => this.Value = value;
        public override BitCount Size => this.Value.Size;

        public override string? ToString() => this.Value.ToString(); 
        
        public override bool ResolvesTo<T>() => this.Value.ResolvesTo<T>();

        public override T As<T>() => this.Value.As<T>();

        public override bool IsCompatibleWith(Base other, bool testAllElements = false) => this.Value
            .IsCompatibleWith(other, testAllElements);

        public override Base MergeWith(Base other) => this.Value.MergeWith(other);
        public override Base Clone() => this;
        public override bool Equals(Base other) => this.Value.Equals(other); 
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            var ot = other as Proxy;
            if (ot != null)
                other = ot.Value;
            this.Value = this.Value.MeetWith(other, ref hasChanged, setToHighestPointer);
            if (ot != null)
                ot.Value = this.Value;
            return this;
        }

        public override bool IsCompatible(Base other, bool testAllElements) => this.Value.IsCompatible(other,
            testAllElements);

        public override Base MeetAt(ByteCount offset, Base other) => this.Value.MeetAt(offset, other);

        public override void Display(object value, StringBuilder builder) => this.Value.Display(value, builder);
    }
}