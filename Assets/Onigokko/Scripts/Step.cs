
public class Step<T>
{
    private T current;
    private T next;
    private T none;

    public Step(T none)
    {
        this.none = none;
        this.current = none;
        this.next = none;
    }

    public void SetNext(T step)
    {
        this.next = step;
    }

    public T GetNext()
    {
        return this.next;
    }

    public T GetCurrent()
    {
        return this.current;
    }

    public T DoTransit()
    {
        if(!this.next.Equals(this.none))
        {
            this.current = this.next;
            this.next = this.none;
            return this.current;
        }
        else
        {
            return this.none;
        }
    }
}
