/**
 * A result type implementation.
 */

export namespace Result {
    export function ok<T, E extends Error = Error>(value: T): Result<T, E> {
        return new Ok(value);
    }

    export function err<T = any, E extends Error = Error>(err: E): Result<T, E> {
        return new Err(err);
    }
}

export type Result<T, E extends Error> = Ok<T, E> | Err<T, E>;

interface Matcher<T, E, U, F> {
    ok: (value: T) => U;
    err: (err: E) => F;
}

interface Variant<T, E extends Error> {
    get isOk(): boolean;
    get isErr(): boolean;

    unwrap(): T;
    unwrapErr(): E;

    bind<U>(fn: (value: T) => Result<U, E>): Result<U, E>;
    map<U>(fn: (value: T) => U): Result<U, E>;
    match<U, F extends Error>(matcher: Matcher<T, E, U, F>): Result<U, F>;
}

class Ok<T, E extends Error> implements Variant<T, E> {
    constructor(private readonly value: T) { }

    public get isOk(): boolean { return true; }
    public get isErr(): boolean { return false; }

    public unwrap(): T { return this.value; }
    public unwrapErr(): E { throw new Error('tried to unwrap the error of an Ok value'); }

    public bind<U>(fn: (value: T) => Result<U, E>): Result<U, E> {
        return fn(this.value);
    }

    public map<U>(fn: (value: T) => U): Result<U, E> {
        return new Ok(fn(this.value));
    }

    public match<U, F extends Error>(matcher: Matcher<T, E, U, F>): Result<U, F> {
        return new Ok(matcher.ok(this.value));
    }
}

class Err<T, E extends Error> implements Variant<T, E> {
    constructor(private readonly err: E) { }

    public get isOk(): boolean { return false; }
    public get isErr(): boolean { return true; }

    public unwrap(): T { throw new Error('tried to unwrap the value of an Err value'); }
    public unwrapErr(): E { return this.err; }

    public bind<U>(fn: (value: T) => Result<U, E>): Result<U, E> {
        return new Err(this.err);
    }

    public map<U>(fn: (value: T) => U): Result<U, E> {
        return new Err(this.err);
    }

    public match<U, F extends Error>(matcher: Matcher<T, E, U, F>): Result<U, F> {
        return new Err(matcher.err(this.err));
    }
}
