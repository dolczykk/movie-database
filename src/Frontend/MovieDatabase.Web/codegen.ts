import type { CodegenConfig } from '@graphql-codegen/cli'

const config: CodegenConfig = {
  schema: '../../../test/MovieDatabase.IntegrationTests/schema.graphql',
  documents: ['graphql/**/*.graphql'],
  ignoreNoDocuments: true,
  generates: {
    'graphql/generated/': {
      preset: 'client',
      config: {
        useTypeImports: true,
        scalars: {
          // The API returns these as ISO-8601 strings over JSON; mapping to
          // `string` reflects the wire format rather than a parsed Date.
          DateTime: 'string',
          LocalDate: 'string',
          UUID: 'string',
          // Not used by any operation yet (uploadBlob), mapped now so a later
          // task doesn't need to revisit this config file.
          Upload: 'File',
        },
      },
    },
  },
}

export default config
