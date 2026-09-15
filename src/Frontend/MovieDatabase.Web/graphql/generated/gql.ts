/* eslint-disable */
import * as types from './graphql';
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';

/**
 * Map of all GraphQL operations in the project.
 *
 * This map has several performance disadvantages:
 * 1. It is not tree-shakeable, so it will include all operations in the project.
 * 2. It is not minifiable, so the string of a GraphQL query will be multiple times inside the bundle.
 * 3. It does not support dead code elimination, so it will add unused operations.
 *
 * Therefore it is highly recommended to use the babel or swc plugin for production.
 * Learn more about it here: https://the-guild.dev/graphql/codegen/plugins/presets/preset-client#reducing-bundle-size
 */
type Documents = {
    "mutation CreateFilm($input: CreateFilmInput!) {\n  createFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation EditFilm($input: EditFilmInput!) {\n  editFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation DeleteFilm($filmId: String!) {\n  deleteFilm(filmId: $filmId)\n}": typeof types.CreateFilmDocument,
    "query GetFilms($first: Int!, $after: String, $where: FilmFilterInput, $order: [FilmSortInput!]) {\n  films(first: $first, after: $after, where: $where, order: $order) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      director {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n    }\n    pageInfo {\n      hasNextPage\n      hasPreviousPage\n      startCursor\n      endCursor\n    }\n  }\n}\n\nquery GetFilmById($id: UUID!) {\n  films(first: 1, where: {id: {eq: $id}}) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      actors {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n      director {\n        id\n        name\n        surname\n      }\n      producer {\n        id\n        name\n      }\n    }\n  }\n}": typeof types.GetFilmsDocument,
};
const documents: Documents = {
    "mutation CreateFilm($input: CreateFilmInput!) {\n  createFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation EditFilm($input: EditFilmInput!) {\n  editFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation DeleteFilm($filmId: String!) {\n  deleteFilm(filmId: $filmId)\n}": types.CreateFilmDocument,
    "query GetFilms($first: Int!, $after: String, $where: FilmFilterInput, $order: [FilmSortInput!]) {\n  films(first: $first, after: $after, where: $where, order: $order) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      director {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n    }\n    pageInfo {\n      hasNextPage\n      hasPreviousPage\n      startCursor\n      endCursor\n    }\n  }\n}\n\nquery GetFilmById($id: UUID!) {\n  films(first: 1, where: {id: {eq: $id}}) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      actors {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n      director {\n        id\n        name\n        surname\n      }\n      producer {\n        id\n        name\n      }\n    }\n  }\n}": types.GetFilmsDocument,
};

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 *
 *
 * @example
 * ```ts
 * const query = graphql(`query GetUser($id: ID!) { user(id: $id) { name } }`);
 * ```
 *
 * The query argument is unknown!
 * Please regenerate the types.
 */
export function graphql(source: string): unknown;

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "mutation CreateFilm($input: CreateFilmInput!) {\n  createFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation EditFilm($input: EditFilmInput!) {\n  editFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation DeleteFilm($filmId: String!) {\n  deleteFilm(filmId: $filmId)\n}"): (typeof documents)["mutation CreateFilm($input: CreateFilmInput!) {\n  createFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation EditFilm($input: EditFilmInput!) {\n  editFilm(input: $input) {\n    id\n    title\n  }\n}\n\nmutation DeleteFilm($filmId: String!) {\n  deleteFilm(filmId: $filmId)\n}"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "query GetFilms($first: Int!, $after: String, $where: FilmFilterInput, $order: [FilmSortInput!]) {\n  films(first: $first, after: $after, where: $where, order: $order) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      director {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n    }\n    pageInfo {\n      hasNextPage\n      hasPreviousPage\n      startCursor\n      endCursor\n    }\n  }\n}\n\nquery GetFilmById($id: UUID!) {\n  films(first: 1, where: {id: {eq: $id}}) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      actors {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n      director {\n        id\n        name\n        surname\n      }\n      producer {\n        id\n        name\n      }\n    }\n  }\n}"): (typeof documents)["query GetFilms($first: Int!, $after: String, $where: FilmFilterInput, $order: [FilmSortInput!]) {\n  films(first: $first, after: $after, where: $where, order: $order) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      director {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n    }\n    pageInfo {\n      hasNextPage\n      hasPreviousPage\n      startCursor\n      endCursor\n    }\n  }\n}\n\nquery GetFilmById($id: UUID!) {\n  films(first: 1, where: {id: {eq: $id}}) {\n    nodes {\n      id\n      title\n      description\n      releaseDate\n      thumbnailUrl\n      actors {\n        id\n        name\n        surname\n      }\n      genres {\n        id\n        name\n      }\n      director {\n        id\n        name\n        surname\n      }\n      producer {\n        id\n        name\n      }\n    }\n  }\n}"];

export function graphql(source: string) {
  return (documents as any)[source] ?? {};
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> = TDocumentNode extends DocumentNode<  infer TType,  any>  ? TType  : never;