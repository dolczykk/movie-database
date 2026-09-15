/* eslint-disable */
/** Internal type. DO NOT USE DIRECTLY. */
type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
/** Internal type. DO NOT USE DIRECTLY. */
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
export type ActorFilterInput = {
  and?: Array<ActorFilterInput> | null | undefined;
  id?: UuidOperationFilterInput | null | undefined;
  name?: StringOperationFilterInput | null | undefined;
  or?: Array<ActorFilterInput> | null | undefined;
  surname?: StringOperationFilterInput | null | undefined;
};

export type ActorPlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
  surname?: string | null | undefined;
};

export type CreateFilmInput = {
  actors: Array<ActorPlaceholderInput>;
  description?: string | null | undefined;
  director: DirectorPlaceholderInput;
  genres: Array<GenrePlaceholderInput>;
  producer: ProducerPlaceholderInput;
  releaseDate: string;
  title: string;
};

export type DirectorInfoFilterInput = {
  and?: Array<DirectorInfoFilterInput> | null | undefined;
  id?: UuidOperationFilterInput | null | undefined;
  name?: StringOperationFilterInput | null | undefined;
  or?: Array<DirectorInfoFilterInput> | null | undefined;
  surname?: StringOperationFilterInput | null | undefined;
};

export type DirectorInfoSortInput = {
  id?: SortEnumType | null | undefined;
  name?: SortEnumType | null | undefined;
  surname?: SortEnumType | null | undefined;
};

export type DirectorPlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
  surname?: string | null | undefined;
};

export type EditFilmActorPlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
  surname?: string | null | undefined;
};

export type EditFilmDirectorPlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
  surname?: string | null | undefined;
};

export type EditFilmGenrePlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
};

export type EditFilmInput = {
  actors: Array<EditFilmActorPlaceholderInput>;
  description?: string | null | undefined;
  director: EditFilmDirectorPlaceholderInput;
  genres: Array<EditFilmGenrePlaceholderInput>;
  id: string;
  producer: EditFilmProducerPlaceholderInput;
  releaseDate: string;
  title: string;
};

export type EditFilmProducerPlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
};

export type FilmFilterInput = {
  actors?: ListFilterInputTypeOfActorFilterInput | null | undefined;
  and?: Array<FilmFilterInput> | null | undefined;
  creatorId?: StringOperationFilterInput | null | undefined;
  description?: StringOperationFilterInput | null | undefined;
  director?: DirectorInfoFilterInput | null | undefined;
  genres?: ListFilterInputTypeOfGenreFilterInput | null | undefined;
  id?: UuidOperationFilterInput | null | undefined;
  or?: Array<FilmFilterInput> | null | undefined;
  producer?: ProducerInfoFilterInput | null | undefined;
  releaseDate?: LocalDateOperationFilterInput | null | undefined;
  title?: StringOperationFilterInput | null | undefined;
};

export type FilmSortInput = {
  creatorId?: SortEnumType | null | undefined;
  description?: SortEnumType | null | undefined;
  director?: DirectorInfoSortInput | null | undefined;
  id?: SortEnumType | null | undefined;
  producer?: ProducerInfoSortInput | null | undefined;
  releaseDate?: SortEnumType | null | undefined;
  title?: SortEnumType | null | undefined;
};

export type GenreFilterInput = {
  and?: Array<GenreFilterInput> | null | undefined;
  id?: UuidOperationFilterInput | null | undefined;
  name?: StringOperationFilterInput | null | undefined;
  or?: Array<GenreFilterInput> | null | undefined;
};

export type GenrePlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
};

export type ListFilterInputTypeOfActorFilterInput = {
  all?: ActorFilterInput | null | undefined;
  any?: boolean | null | undefined;
  none?: ActorFilterInput | null | undefined;
  some?: ActorFilterInput | null | undefined;
};

export type ListFilterInputTypeOfGenreFilterInput = {
  all?: GenreFilterInput | null | undefined;
  any?: boolean | null | undefined;
  none?: GenreFilterInput | null | undefined;
  some?: GenreFilterInput | null | undefined;
};

export type LocalDateOperationFilterInput = {
  eq?: string | null | undefined;
  gt?: string | null | undefined;
  gte?: string | null | undefined;
  in?: Array<string | null | undefined> | null | undefined;
  lt?: string | null | undefined;
  lte?: string | null | undefined;
  neq?: string | null | undefined;
  ngt?: string | null | undefined;
  ngte?: string | null | undefined;
  nin?: Array<string | null | undefined> | null | undefined;
  nlt?: string | null | undefined;
  nlte?: string | null | undefined;
};

export type ProducerInfoFilterInput = {
  and?: Array<ProducerInfoFilterInput> | null | undefined;
  id?: UuidOperationFilterInput | null | undefined;
  name?: StringOperationFilterInput | null | undefined;
  or?: Array<ProducerInfoFilterInput> | null | undefined;
};

export type ProducerInfoSortInput = {
  id?: SortEnumType | null | undefined;
  name?: SortEnumType | null | undefined;
};

export type ProducerPlaceholderInput = {
  id?: string | null | undefined;
  name?: string | null | undefined;
};

export type SortEnumType =
  | 'ASC'
  | 'DESC';

export type StringOperationFilterInput = {
  and?: Array<StringOperationFilterInput> | null | undefined;
  contains?: string | null | undefined;
  endsWith?: string | null | undefined;
  eq?: string | null | undefined;
  in?: Array<string | null | undefined> | null | undefined;
  ncontains?: string | null | undefined;
  nendsWith?: string | null | undefined;
  neq?: string | null | undefined;
  nin?: Array<string | null | undefined> | null | undefined;
  nstartsWith?: string | null | undefined;
  or?: Array<StringOperationFilterInput> | null | undefined;
  startsWith?: string | null | undefined;
};

export type UuidOperationFilterInput = {
  eq?: string | null | undefined;
  gt?: string | null | undefined;
  gte?: string | null | undefined;
  in?: Array<string | null | undefined> | null | undefined;
  lt?: string | null | undefined;
  lte?: string | null | undefined;
  neq?: string | null | undefined;
  ngt?: string | null | undefined;
  ngte?: string | null | undefined;
  nin?: Array<string | null | undefined> | null | undefined;
  nlt?: string | null | undefined;
  nlte?: string | null | undefined;
};

export type CreateFilmMutationVariables = Exact<{
  input: CreateFilmInput;
}>;


export type CreateFilmMutation = { createFilm: { id: string, title: string } };

export type EditFilmMutationVariables = Exact<{
  input: EditFilmInput;
}>;


export type EditFilmMutation = { editFilm: { id: string, title: string } };

export type DeleteFilmMutationVariables = Exact<{
  filmId: string;
}>;


export type DeleteFilmMutation = { deleteFilm: string };

export type GetFilmsQueryVariables = Exact<{
  first: number;
  after?: string | null | undefined;
  where?: FilmFilterInput | null | undefined;
  order?: Array<FilmSortInput> | FilmSortInput | null | undefined;
}>;


export type GetFilmsQuery = { films: { nodes: Array<{ id: string, title: string, description: string | null, releaseDate: string, thumbnailUrl: string | null, director: { id: string, name: string, surname: string }, genres: Array<{ id: string, name: string }> }> | null, pageInfo: { hasNextPage: boolean, hasPreviousPage: boolean, startCursor: string | null, endCursor: string | null } } | null };

export type GetFilmByIdQueryVariables = Exact<{
  id: string;
}>;


export type GetFilmByIdQuery = { films: { nodes: Array<{ id: string, title: string, description: string | null, releaseDate: string, thumbnailUrl: string | null, actors: Array<{ id: string, name: string, surname: string }>, genres: Array<{ id: string, name: string }>, director: { id: string, name: string, surname: string }, producer: { id: string, name: string } }> | null } | null };


export const CreateFilmDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateFilm"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateFilmInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createFilm"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}}]}}]}}]} as unknown as DocumentNode<CreateFilmMutation, CreateFilmMutationVariables>;
export const EditFilmDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"EditFilm"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"EditFilmInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"editFilm"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}}]}}]}}]} as unknown as DocumentNode<EditFilmMutation, EditFilmMutationVariables>;
export const DeleteFilmDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteFilm"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"filmId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteFilm"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"filmId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"filmId"}}}]}]}}]} as unknown as DocumentNode<DeleteFilmMutation, DeleteFilmMutationVariables>;
export const GetFilmsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetFilms"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"first"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"after"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"where"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"FilmFilterInput"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"order"}},"type":{"kind":"ListType","type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"FilmSortInput"}}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"films"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"first"},"value":{"kind":"Variable","name":{"kind":"Name","value":"first"}}},{"kind":"Argument","name":{"kind":"Name","value":"after"},"value":{"kind":"Variable","name":{"kind":"Name","value":"after"}}},{"kind":"Argument","name":{"kind":"Name","value":"where"},"value":{"kind":"Variable","name":{"kind":"Name","value":"where"}}},{"kind":"Argument","name":{"kind":"Name","value":"order"},"value":{"kind":"Variable","name":{"kind":"Name","value":"order"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"nodes"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"releaseDate"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnailUrl"}},{"kind":"Field","name":{"kind":"Name","value":"director"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"surname"}}]}},{"kind":"Field","name":{"kind":"Name","value":"genres"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"pageInfo"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"hasNextPage"}},{"kind":"Field","name":{"kind":"Name","value":"hasPreviousPage"}},{"kind":"Field","name":{"kind":"Name","value":"startCursor"}},{"kind":"Field","name":{"kind":"Name","value":"endCursor"}}]}}]}}]}}]} as unknown as DocumentNode<GetFilmsQuery, GetFilmsQueryVariables>;
export const GetFilmByIdDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetFilmById"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"films"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"first"},"value":{"kind":"IntValue","value":"1"}},{"kind":"Argument","name":{"kind":"Name","value":"where"},"value":{"kind":"ObjectValue","fields":[{"kind":"ObjectField","name":{"kind":"Name","value":"id"},"value":{"kind":"ObjectValue","fields":[{"kind":"ObjectField","name":{"kind":"Name","value":"eq"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}]}}]}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"nodes"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"releaseDate"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnailUrl"}},{"kind":"Field","name":{"kind":"Name","value":"actors"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"surname"}}]}},{"kind":"Field","name":{"kind":"Name","value":"genres"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}},{"kind":"Field","name":{"kind":"Name","value":"director"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"surname"}}]}},{"kind":"Field","name":{"kind":"Name","value":"producer"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}}]}}]}}]}}]} as unknown as DocumentNode<GetFilmByIdQuery, GetFilmByIdQueryVariables>;